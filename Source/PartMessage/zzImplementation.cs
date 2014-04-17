using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using System.Linq.Expressions;
using System.Collections;
using System.Text.RegularExpressions;
using KSPAPIExtensions.PartMessage;

namespace KSPAPIExtensions.PartMessage
{
    internal class ServiceImpl : MonoBehaviour, PartMessageService
    {

        #region MessageSourceInfo

        public CurrentMessageInfo SourceInfo
        {
            get { return sourceInfo; }
        }

        private SourceInfoImpl sourceInfo;

        internal class SourceInfoImpl : CurrentMessageInfo
        {
            internal SourceInfoImpl() { }

            public Type message
            {
                get { return curr.Peek().message; }
            }

            public IEnumerable<Type> allMessages
            {
                get
                {
                    return curr.Peek();
                }
            }

            public object source
            {
                get
                {
                    return curr.Peek().source;
                }
            }

            public Part part
            {
                get
                {
                    return curr.Peek().part;
                }
            }

            public PartModule srcModule
            {
                get { return source as PartModule; }
            }

            public PartRelationship SourceRelationTo(Part destPart)
            {
                Part src = part;
                if (src == null)
                    return PartRelationship.Unknown;
                return src.RelationTo(destPart);
            }

            #region Internal Bits
            private Stack<Info> curr = new Stack<Info>();

            internal IDisposable Push(object source, Part part, Type message)
            {
                return new Info(this, source, part, message);
            }

            private class Info : MessageEnumerable, IDisposable
            {
                internal Info(SourceInfoImpl info, object source, Part part, Type message)
                    : base(message)
                {
                    this.source = source;
                    this.part = part;
                    this.info = info;
                    info.curr.Push(this);
                }

                readonly internal SourceInfoImpl info;
                readonly internal Part part;
                readonly internal object source;

                void IDisposable.Dispose()
                {
                    info.curr.Pop();
                }

            }
            #endregion
        }
        #endregion

        #region Object scanning

        /// <summary>
        /// Scan an object for message events and message listeners and hook them up.
        /// Note that all references are dumped on game scene change, so objects must be rescanned when reloaded.
        /// </summary>
        /// <param name="obj">the object to scan</param>
        public void ScanObject<T>(T obj)
        {
            Type t = typeof(T);

            foreach (MethodInfo meth in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                foreach (PartMessageListener attr in meth.GetCustomAttributes(typeof(PartMessageListener), true))
                    AddListener(obj, meth, attr);
            }

            foreach (EventInfo evt in t.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Type deleg = evt.EventHandlerType;
                if (evt.GetCustomAttributes(typeof(PartMessageEvent), true).Length == 0)
                {
                    // sanity check
                    if (deleg.GetCustomAttributes(typeof(PartMessage), true).Length > 0)
                        Debug.LogWarning(string.Format("[PartMessageService] Event: {0} in class: {1} declares an event with a part message, but does not have the PartMessageEvent attribute. Will ignore", evt.Name, t.FullName));
                    continue;
                }
                foreach (PartMessage attr in deleg.GetCustomAttributes(typeof(PartMessage), true))
                    GenerateEventHandoff(obj, evt);
            }
        }
        #endregion

        #region Listeners

        private Dictionary<string, LinkedList<ListenerInfo>> listeners = new Dictionary<string, LinkedList<ListenerInfo>>();

        private class ListenerInfo
        {
            public WeakReference targetRef;
            public MethodInfo method;
            public PartMessageListener attr;

            public LinkedListNode<ListenerInfo> node;

            public object target
            {
                get
                {
                    return targetRef.Target;
                }
            }

            public Part part
            {
                get
                {
                    object target = this.target;
                    return AsPart(target);
                }
            }

            public PartModule module
            {
                get
                {
                    return target as PartModule;
                }
            }
        }

        private void AddListener(object target, MethodInfo meth, PartMessageListener attr)
        {
            //Debug.LogWarning(string.Format("[PartMessageUtils] {0}.{1} Adding other for {2}", target.GetType().Name, meth.Name, attr.message.FullName));

            if (!attr.scenes.IsLoaded())
                return;

            string message = attr.message.FullName;
            if (Delegate.CreateDelegate(attr.message, target, meth, false) == null)
            {
                Debug.LogError(string.Format("PartMessageListener method {0}.{1} does not support the delegate type {2} as declared in the attribute", meth.DeclaringType, meth.Name, attr.message.Name));
                return;
            }

            LinkedList<ListenerInfo> listenerList;
            if (!listeners.TryGetValue(message, out listenerList))
            {
                listenerList = new LinkedList<ListenerInfo>();
                listeners.Add(message, listenerList);
            }

            ListenerInfo info = new ListenerInfo();
            info.targetRef = new WeakReference(target);
            info.method = meth;
            info.attr = attr;
            info.node = listenerList.AddLast(info);
        }

        // Do not change the signature of this method. It will be called using reflection by old versions
        internal void MasterSendMessageV1(object source, Part part, Type message, object[] args)
        {
            if (!gameObject)
                return;

            //Debug.LogWarning(string.Format("MasterSendMessageV1({0}, {1}, {2}, {3}, {4})", source, part, message, args, (args == null) ? -1 : args.Length));

            if (masterFilters != null)
                foreach (MasterFilterInfo info in masterFilters)
                    if (info.DoFilter(source, part, message, args))
                        return;

            SendMessageInServices(source, part, message, args);
        }

        // This will be called from the master to send messages
        // In the unlikley event that the signature of this method needs to change
        // future versions will figure out the right one to invoke.
        internal void ServiceSendMessageV1(object source, Part part, Type message, object[] args)
        {
            // Check for destruction.
            if (!gameObject)
                return;

            message = TranslateMessage(message);

            using (sourceInfo.Push(source, part, message))
            {
                // Send the message
                foreach (Type messageCls in sourceInfo.allMessages)
                {
                    string messageName = messageCls.FullName;

                    LinkedList<ListenerInfo> listenerList;
                    if (!listeners.TryGetValue(messageName, out listenerList))
                        continue;

                    // Shorten parameter list if required
                    object[] newArgs = null;

                    for (var node = listenerList.First; node != null; )
                    {
                        // hold reference for duration of call
                        ListenerInfo info = node.Value;
                        object target = info.target;
                        if (target == null)
                        {
                            // Remove dead links from the list
                            var tmp = node;
                            node = node.Next;
                            listenerList.Remove(tmp);
                            continue;
                        }

                        // Declarative event filtering
                        PartModule module = info.module;
                        if ((module == null || (module.isEnabled && module.enabled))
                            && info.attr.scenes.IsLoaded()
                            && PartUtils.RelationTest(sourceInfo.part, info.part, info.attr.relations)) 
                        {
                            try
                            {
                                node.Value.method.Invoke(target, newArgs ?? (newArgs = ShortenArgs(args, messageCls)));
                            }
                            catch (TargetException ex)
                            {
                                // Swallow target exceptions, but not anything else.
                                Debug.LogError(string.Format("Invoking {0}.{1} to handle message {2} resulted in an exception.", target.GetType(), node.Value.method, sourceInfo.message));
                                Debug.LogException(ex.InnerException);
                            }
                        }

                        node = node.Next;
                    }

                }
            }
        }

        private static object[] ShortenArgs(object[] args, Type messageCls)
        {
            ParameterInfo[] methodParams = messageCls.GetMethod("Invoke").GetParameters();
            object[] newArgs = args;
            if (args.Length > methodParams.Length)
            {
                newArgs = new object[methodParams.Length];
                Array.Copy(args, newArgs, methodParams.Length);
            }
            return newArgs;
        }

        #endregion

        #region Event Binding and Dynamic Message Sending

        /// <summary>
        /// Send a message. Normally this will be automatically invoked by the event, but there are types when dynamic invocation is required.
        /// </summary>
        /// <param name="source">Source of the message. This should be either a Part or a PartModule.</param>
        /// <param name="message">The message delegate type. This must have the PartMessage attribute.</param>
        /// <param name="args">message arguments</param>
        public void SendPartMessage(object source, Type message, params object[] args)
        {
            SendPartMessage(source, AsPart(source), message, args);
        }

        /// <summary>
        /// Send a message. Normally this will be automatically invoked by the event, but there are types when dynamic invocation is required.
        /// This version allows the source to proxy for some other part.
        /// </summary>
        /// <param name="source">Source of the message. This may be any object</param>
        /// <param name="part">Part that the message source is proxying for</param>
        /// <param name="message">The message delegate type. This must have the PartMessage attribute.</param>
        /// <param name="args">message arguments</param>
        public void SendPartMessage(object source, Part part, Type message, params object[] args)
        {
            if (message.GetCustomAttributes(typeof(PartMessage), true).Length == 0)
                throw new ArgumentException("Message does not have PartMessage attribute", "message");

            SendMessageInMaster(source, part, message, args);
        }

        private void GenerateEventHandoff(object source, EventInfo evt)
        {
            MethodAttributes addAttrs = evt.GetAddMethod(true).Attributes;

            Part part = AsPart(source);

            // This generates a dynamic method that pulls the properties of the event
            // plus the arguments passed and hands it off to the EventHandler method below.
            Type message = evt.EventHandlerType;
            MethodInfo m = message.GetMethod("Invoke", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            //Debug.LogWarning(string.Format("[PartMessageUtils] {0}.{1} Adding event handler for {2}", source.GetType().Name, evt.Name, message.FullName));

            ParameterInfo[] pLst = m.GetParameters();
            ParameterExpression[] peLst = new ParameterExpression[pLst.Length];
            Expression[] cvrt = new Expression[pLst.Length];
            for (int i = 0; i < pLst.Length; i++)
            {
                peLst[i] = Expression.Parameter(pLst[i].ParameterType, pLst[i].Name);
                cvrt[i] = Expression.Convert(peLst[i], typeof(object));
            }
            Expression createArr = Expression.NewArrayInit(typeof(object), cvrt);

            Expression invoke = Expression.Call(Expression.Constant(SendMessageInMaster), SendMessageInMaster.GetType().GetMethod("Invoke"),
                Expression.Constant(source), Expression.Constant(part), Expression.Constant(message), createArr);

            Delegate d = Expression.Lambda(message, invoke, peLst).Compile();

            // Shouldn't need to use a weak delegate here.
            evt.AddEventHandler(source, d);
        }

        #endregion

        #region Message Filters

        /// <summary>
        /// Register a message filter. This delegate will be called for every message sent from the source.
        /// If it returns true, the message is considered handled and no futher processing will occour.
        /// </summary>
        /// <param name="filter">The delegate for the filter</param>
        /// <param name="source">Message source, must match. If null will match all sources.</param>
        /// <param name="part">Part to filter. If null will match all parts.</param>
        /// <param name="messages">Optional list of messages to match. If empty, all messages are matched.</param>
        /// <returns>Disposable object. When done call dispose. Works well with using clauses.</returns>
        public IDisposable MessageFilter(PartMessageFilter filter, object source = null, Part part = null, params Type[] messages)
        {
            FilterInfo info = new FilterInfo();
            info.Filter = filter;

            RegisterFilterInfo(source, part, messages, info);

            return info;
        }

        /// <summary>
        /// Consolidate messages. All messages sent by the source will be held until the returned object is destroyed.
        /// Any duplicates of the same message will be swallowed silently.
        /// </summary>
        /// <param name="source">source to consolidate from. Null will match all sources</param>
        /// <param name="part">Part to filter. If null will match all parts.</param>
        /// <param name="messages">messages to consolidate. If not specified, all messages are consolidated.</param>
        /// <returns>Disposable object. When done call dispose. Works well with using clauses.</returns>
        public IDisposable MessageConsolidate(object source = null, Part part = null, params Type[] messages)
        {
            FilterInfo consolidator = new MessageConsolidator();

            RegisterFilterInfo(source, part, messages, consolidator);

            return consolidator;
        }

        /// <summary>
        /// Ignore messages sent by the source until the returned object is destroyed.
        /// </summary>
        /// <param name="source">Source to ignore. Null will ignore all sources.</param>
        /// <param name="part">Part to filter. If null will match all parts.</param>
        /// <param name="messages">Messages to ignore. If not specified, all messages are ignored.</param>
        /// <returns>Disposable object. When done call dispose. Works well with using clauses.</returns>
        public IDisposable MessageIgnore(object source = null, Part part = null, params Type[] messages)
        {
            return MessageFilter((src, pa, message, args) => true, source, part, messages);
        }

        private void RegisterFilterInfo(object source, Part part, Type[] messages, FilterInfo info)
        {
            info.source = source;
            info.part = part;
            info.service = this;

            foreach (Type root in messages)
                foreach (Type message in new MessageEnumerable(root))
                    info.messages.Add(message.FullName);

            info.register = RegisterFilterInMaster(this, info.DoFilter);
        }

        internal class FilterInfo : IDisposable
        {
            public object source;
            public Part part;
            public PartMessageFilter Filter;
            public HashSet<string> messages = new HashSet<string>();

            public ServiceImpl service;

            public IDisposable register;

            // Don't change this method signature, it is called by the master.
            internal bool DoFilter(object source, Part part, Type message, object[] args)
            {
                //Debug.LogWarning(string.Format("DoFilter({0}, {1}, {2}, {3}, {4})", source, part, message, args, (args == null) ? -1 : args.Length));

                if (this.source != null && this.source != source)
                    return false;
                if (this.part != null && this.part != part)
                    return false;
                if (this.messages.Count > 0 && !this.messages.Contains(message.FullName))
                    return false;

                message = TranslateMessage(message);

                using (service.sourceInfo.Push(source, part, message))
                {
                    return this.Filter(source, part, message, args);
                }
            }

            public virtual void Dispose()
            {
                register.Dispose();
            }
        }

        internal class MessageConsolidator : FilterInfo
        {
            public MessageConsolidator()
            {
                this.Filter = ConsolidatingFilter;
            }

            private class PartMessageInfo : IEquatable<PartMessageInfo>
            {
                public object source;
                public Part part;
                public Type message;
                public object[] args;

                private readonly int hashCode;

                public PartMessageInfo(object source, Part part, Type message, object[] args)
                {
                    this.source = source;
                    this.part = part;
                    this.message = message;
                    this.args = args;
                    hashCode = (source==null?0:source.GetHashCode()) ^ (part==null?0:part.GetHashCode()) ^ message.FullName.GetHashCode() ^ args.Length;
                    foreach (object arg in args)
                        hashCode ^= (arg==null?0:arg.GetHashCode());
                }

                public override bool Equals(object obj)
                {
                    return base.Equals(obj as PartMessageInfo);
                }

                public bool Equals(PartMessageInfo other)
                {
                    if (other == null)
                        return false;
                    if (other == this)
                        return true;

                    if (hashCode != other.hashCode)
                        return false;
                    if (source != other.source)
                        return false;
                    if (message.FullName != other.message.FullName)
                        return false;
                    if (args.Length != other.args.Length)
                        return false;
                    for (int i = 0; i < args.Length; i++)
                        if (!args[i].Equals(other.args[i]))
                            return false;
                    return true;
                }

                public override int GetHashCode()
                {
                    return hashCode;
                }
            }

            private HashSet<PartMessageInfo> messageSet = new HashSet<PartMessageInfo>();
            private List<PartMessageInfo> messageList = new List<PartMessageInfo>();

            private bool ConsolidatingFilter(object source, Part part, Type message, object[] args)
            {
                var info = new PartMessageInfo(source, part, message, args);
                if (messageSet.Add(info))
                    messageList.Add(info);
                return true;
            }

            public override void Dispose()
            {
                base.Dispose();

                // Safe as we've already deregistered the filter, so no loops.
                foreach (PartMessageInfo message in messageList)
                    service.SendPartMessage(message.source, message.part, message.message, message.args);

                messageSet = null;
                messageList = null;
            }

        }

        private LinkedList<MasterFilterInfo> masterFilters;

        private class MasterFilterInfo : IDisposable
        {
            public Func<object, Part, Type, object[], bool> DoFilter;
            public LinkedListNode<MasterFilterInfo> node;

            public void Dispose()
            {
                node.List.Remove(node);
            }
        }

        // Do not change the signature of this method. It will be called using reflection by old versions
        internal IDisposable MasterRegisterFilterV1(object service, Func<object, Part, Type, object[], bool> filter)
        {
            // If there ends up being some version dependency with the service, the service and all its bits are there to fiddle with.
            if (!isMaster)
                throw new InvalidProgramException("Calling filter registration in target that isn't the master");

            MasterFilterInfo info = new MasterFilterInfo { DoFilter = filter };
            info.node = masterFilters.AddFirst(info);
            return info;
        }


        #endregion

        #region Other version handling

        private bool isMaster { get { return SendMessageInServices != null;  } }

        private Func<object, Func<object, Part, Type, object[], bool>, IDisposable> RegisterFilterInMaster;
        private Action<object, Part, Type, object[]> SendMessageInMaster;

        private Action<object, Part, Type, object[]> SendMessageInServices;

        // Do not change the signature of this method. It will be called using reflection by old versions
        internal void SetMaster(object master, List<object> otherVersions)
        {
            if (master == (object)this)
            {
                masterFilters = new LinkedList<MasterFilterInfo>();

                RegisterFilterInMaster = MasterRegisterFilterV1;
                SendMessageInMaster = MasterSendMessageV1;

                // Note: this may need modifying some day. 
                SendMessageInServices = ServiceSendMessageV1;
                foreach (object service in otherVersions)
                    SendMessageInServices += (Action<object, Part, Type, object[]>)CreateMethodDelegate(typeof(Action<object, Part, Type, object[]>), service, "ServiceSendMessageV1");
            }
            else
            {
                if (masterFilters != null)
                    throw new InvalidProgramException("Shouldn't be possible to change target once filters are registered");

                // Need to call the winner of the election for MasterRegisterFilter through reflection.
                RegisterFilterInMaster = (Func<object, Func<object, Part, Type, object[], bool>, IDisposable>)CreateMethodDelegate(typeof(Func<object, Func<object, Part, Type, object[], bool>, IDisposable>), master, "MasterRegisterFilterV1");
                SendMessageInMaster = (Action<object, Part, Type, object[]>)CreateMethodDelegate(typeof(Action<object, Part, Type, object[]>), master, "MasterSendMessageV1");
                SendMessageInServices = null;
            }
            
        }

        #endregion

        #region Startup

        private EventData<GameScenes>.OnEvent sceneChangedListener;

        internal void Awake() 
        {
            // Create source info
            sourceInfo = new SourceInfoImpl();

            // Clear the listeners list when reloaded.
            sceneChangedListener = scene => listeners.Clear();
            GameEvents.onGameSceneLoadRequested.Add(sceneChangedListener);

            // Build a dictionary of local messages
            localMessages = (from type in GetType().Assembly.GetTypes()
                             where typeof(Delegate).IsAssignableFrom(type) &&
                             type.GetCustomAttributes(typeof(PartMessageEvent), true).Length > 0
                             select type)
                             .ToDictionary(t => t.FullName);

            HashSet<string> assem = new HashSet<string>();
            foreach (var a in AssemblyLoader.loadedAssemblies)
                assem.Add(a.assembly.GetName().Name);
            
            foreach (UrlDir.UrlConfig urlConf in GameDatabase.Instance.root.AllConfigs)
            {
                if (urlConf.type != "PART")
                    continue;

                ConfigNode part = urlConf.config;
                if(CheckPartRequiresAssembly(assem, part))
                    continue;
            }
        }

        private static bool CheckPartRequiresAssembly(HashSet<string> assem, ConfigNode part)
        {
            string partName = part.GetValue("name");

            foreach (string keyValue in part.GetValues("RequiresAssembly"))
            {
                foreach (string split in keyValue.Split(','))
                {
                    string value = split.Trim();
                    if (!string.IsNullOrEmpty(value) && (value[0] == '!' ? assem.Contains(value.Substring(1).Trim()) : !assem.Contains(value)))
                    {
                        Debug.Log("[PartMessageService] removing part " + partName + " due to dependency requirements not met");
                        part.ClearData();
                        return true;
                    }
                }
            }

            part.RemoveValues("RequiresAssembly");
            return false;
        }

        private static bool CheckPartNeedsManager(ConfigNode part, Dictionary<string, bool> requiresManagerCache)
        {
            string partModule = part.GetValue("module");

            if (NeedsManager(typeof(Part), partModule, requiresManagerCache))
                return true;

            foreach (ConfigNode module in part.GetNodes("MODULE"))
            {
                string moduleName = module.GetValue("name");
                if (NeedsManager(typeof(PartModule), moduleName, requiresManagerCache))
                    return true;
            }

            return false;
        }

        private static bool NeedsManager(Type parentType, string typeName, Dictionary<string, bool> requiresManagerCache)
        {
            bool cached;
            if (requiresManagerCache.TryGetValue(typeName, out cached))
                return cached;
            
            Type type = AssemblyLoader.GetClassByName(parentType, typeName);
            if (type == null)
                return false;

            return requiresManagerCache[typeName] = NeedsManager(type);
        }

        private static bool NeedsManager(Type type)
        {
            foreach (EventInfo info in type.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                if (info.GetCustomAttributes(typeof(PartMessageEvent), true).Length > 0)
                    return true;

            foreach (MethodInfo meth in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                if (meth.GetCustomAttributes(typeof(PartMessageListener), true).Length > 0)
                    return true;

            return false;
        }

        internal void OnDestroy()
        {
            GameEvents.onGameSceneLoadRequested.Remove(sceneChangedListener);
            sceneChangedListener = null;
            listeners = null;
            masterFilters = null;
            SendMessageInServices = null;
        }

        #endregion

        #region Utility Functions

        private static Dictionary<string, Type> localMessages; 

        private static Type TranslateMessage(Type message)
        {
            // Translate the message into the local assembly
            Type localMessage;
            if(localMessages.TryGetValue(message.FullName, out localMessage))
                return localMessage;
            return message;
        }

        private static Delegate CreateMethodDelegate(Type dlgType, object target, string name)
        {
            MethodInfo invokeMethod = dlgType.GetMethod("Invoke");
            Type[] args = (from p in invokeMethod.GetParameters()
                           select p.ParameterType).ToArray();

            MethodInfo info = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.ExactBinding, null, args, null);
            return Delegate.CreateDelegate(dlgType, info);
        }

        private static Part AsPart(object src) 
        {
            if (src is Part)
                return (Part)src;
            if (src is PartModule)
                return ((PartModule)src).part;
            if (src is PartMessagePartProxy)
                return ((PartMessagePartProxy)src).proxyPart;
            return null;
        }
        #endregion
    }

    #region Message Enumerator
    internal class MessageEnumerable : IEnumerable<Type>
    {
        internal MessageEnumerable(Type message)
        {
            this.message = message;
        }

        readonly internal Type message;

        IEnumerator<Type> IEnumerable<Type>.GetEnumerator()
        {
            return new MessageEnumerator(message);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new MessageEnumerator(message);
        }

        private class MessageEnumerator : IEnumerator<Type>
        {

            public MessageEnumerator(Type top)
            {
                this.current = this.top = top;
            }

            private int pos = -1;
            private Type current;
            private Type top;

            object IEnumerator.Current
            {
                get
                {
                    if (pos != 0)
                        throw new InvalidOperationException();
                    return current;
                }
            }

            Type IEnumerator<Type>.Current
            {
                get
                {
                    if (pos != 0)
                        throw new InvalidOperationException();
                    return current;
                }
            }

            bool IEnumerator.MoveNext()
            {
                switch (pos)
                {
                    case -1:
                        current = top;
                        pos = 0;
                        break;
                    case 1:
                        return false;
                    case 0:
                        PartMessage evt = (PartMessage)current.GetCustomAttributes(typeof(PartMessage), true)[0];
                        current = evt.parent;
                        break;
                    case 2:
                        throw new InvalidOperationException("Enumerator disposed");
                }
                if (current == null)
                {
                    pos = 1;
                    return false;
                }
                return true;
            }

            void IEnumerator.Reset()
            {
                pos = -1;
                current = null;
            }

            void IDisposable.Dispose()
            {
                current = top = null;
                pos = 2;
            }
        }
    }
    #endregion

}
