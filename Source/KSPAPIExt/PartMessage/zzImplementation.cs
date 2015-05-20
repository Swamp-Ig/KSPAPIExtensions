using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Linq.Expressions;
using System.Collections;
using DeftTech.DuckTyping;

namespace KSPAPIExtensions.PartMessage
{
    #region Duck Typing interfaces
    /// <summary>
    /// Interface to allow duck casting of message listeners.
    /// 
    /// <b>Do not change this interface or duck casting will fail.</b>
    /// </summary>
    internal interface IPartMessageListenerV1
    {
        Type DelegateType { get; }

        GameSceneFilter Scenes { get; }

        PartRelationship Relations { get; }
    }

    /// <summary>
    /// Interface to allow duck casting of messages.
    /// 
    /// <b>Do not change this interface or duck casting will fail.</b>
    /// </summary>
    internal interface IPartMessageDelegateV1
    {
        Type Parent { get; }

        bool IsAbstract { get; }
    }

    /// <summary>
    /// Interface to allow duck casting of messages.
    /// 
    /// <b>Do not change this interface or duck casting will fail.</b>
    /// </summary>
    internal interface IPartMessageEventV1
    {
        bool IsAsync { get; }
    }

    #endregion

    #region Current Event Info
    internal class CurrentEventInfoImpl : ICurrentEventInfo, IDisposable
    {
        #region Internal Bits
        [ThreadStatic]
        internal static CurrentEventInfoImpl current;

#if DEBUG
        private bool onStack;
#endif
        private CurrentEventInfoImpl previous;
        internal bool filterComplete;

        internal CurrentEventInfoImpl(IPartMessage message, object source, Part part, object[] args)
        {
            Source = source;
            SourcePart = part;
            Message = message;
            Arguments = args;

            List<object> identityArgs = new List<object>();
            ParameterInfo [] paramInfos = message.DelegateType.GetMethod("Invoke").GetParameters();
            for(int i = 0; i < paramInfos.Length; ++i)
            {
                foreach(Attribute attr in paramInfos[i].GetCustomAttributes(false))
                    if(attr.GetType().FullName == typeof(UseLatest).FullName)
                        goto foundAttr;
                identityArgs.Add(args[i]);
            foundAttr:
                ;
            }
            IdentArguments = identityArgs.AsReadOnly();
        }

        internal IDisposable Push()
        {
#if DEBUG
            if(onStack)
                throw new InvalidProgramException("Pushing message onto the stack when it's already on it");
            onStack = true;
#endif

            previous = current;
            current = this;
            return this;
        }

        void IDisposable.Dispose()
        {
#if DEBUG
            if (!onStack)
                throw new InvalidProgramException("Disposed called when not on the stack.");
            onStack = false;
#endif

            current = previous;
            previous = null;
        }

#if DEBUG
        ~CurrentEventInfoImpl()
        {
            if (onStack)
            {
                Debug.LogError("CurrentEventInfoImpl somehow left on the call stack");
            }

        }
#endif

        #endregion

        #region Interface methods

        public IPartMessage Message { get; private set; }

        public object Source { get; private set; }

        public Part SourcePart { get; private set; }

        public object[] Arguments { get; private set; }

        public IEnumerable<object> IdentArguments { get; private set; }

        public PartModule SourceModule { get { return Source as PartModule; } }

        public PartRelationship SourceRelationTo(Part destPart)
        {
            // ReSharper disable once InvokeAsExtensionMethod  Don't because SourcePart may be null
            return PartUtils.RelationTo(SourcePart, destPart);
        }

        public override string ToString()
        {
            return string.Format("CurrentEventInfoImpl(Message:{0}, Source:{1}, SourcePart:{2}, Arguments.Length={3})", Message, Source, SourcePart, (Arguments == null) ? -1 : Arguments.Length);
        }

        public bool Equals(ICurrentEventInfo other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;

            if (GetHashCode() != other.GetHashCode())
                return false;

            if (Source != other.Source)
                return false;
            if (Message.Name != other.Message.Name)
                return false;
            if (!IdentArguments.SequenceEqual(other.IdentArguments))
                return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ICurrentEventInfo);
        }

        private int hashCode;

        public override int GetHashCode()
        {
            // ReSharper disable NonReadonlyFieldInGetHashCode
            if (hashCode != 0)
                return hashCode;

            hashCode =
                Source.GetHashCode()
                ^ ((SourcePart == null) ? 0 : SourcePart.GetHashCode())
                ^ Message.Name.GetHashCode()
                ^ Arguments.Length;
            foreach (object arg in IdentArguments)
                hashCode ^= (arg == null ? 0 : arg.GetHashCode());
            return hashCode;
            // ReSharper restore NonReadonlyFieldInGetHashCode
        }

        #endregion

    }

    #endregion

    #region Part Message
    internal class MessageImpl : IPartMessage
    {
        private readonly IPartMessageDelegateV1 ifMsg;
        internal MessageImpl parent;

        internal MessageImpl(ServiceImpl service, Type message)
        {
            if (!typeof(Delegate).IsAssignableFrom(message))
                throw new ArgumentException("Message type " + message + " is not a delegate type");

            Attribute attribute;
            foreach(Attribute attr in message.GetCustomAttributes(false))
                if (attr.GetType().FullName == typeof(PartMessageDelegate).FullName)
                {
                    attribute = attr;
                    goto foundAttribute;
                }
            throw new ArgumentException("Message does not have the PartMessageDelegate attribute");

            foundAttribute:
            DelegateType = message;

            ifMsg = ServiceImpl.AsDelegate(attribute);
            if(ifMsg.Parent != null)
                parent = (MessageImpl)service.AsIPartMessage(ifMsg.Parent);
        }

        public string Name
        {
            get { return DelegateType.FullName; }
        }

        public Type DelegateType
        {
            get;
            private set;
        }

        public IPartMessage Parent
        {
            get { return parent; }
        }

        public bool IsAbstract
        {
            get { return ifMsg.IsAbstract; }
        }

        public IEnumerator<IPartMessage> GetEnumerator()
        {
            return new Enumerator
            {
                head = this
            };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class Enumerator : IEnumerator<IPartMessage>
        {
            internal MessageImpl head;
            private MessageImpl current;
            private bool atEnd;

            public IPartMessage Current
            {
                get 
                {
                    if(head == null)
                        throw new InvalidOperationException("Iterator disposed");
                    if (current == null)
                        throw new InvalidOperationException("Iterator is at " + (atEnd?"end":"start"));
                    return current;
                }
            }

            public void Dispose()
            {
                current = head = null;
                atEnd = true;
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                if (head == null)
                    throw new InvalidOperationException("Iterator disposed");
                if (atEnd)
                    throw new InvalidOperationException("Iterator is at end");
                
                current = current == null ? head : current.parent;
                
                return !(atEnd = (current == null));
            }

            public void Reset()
            {
                current = null;
                atEnd = false;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
    #endregion

    internal class ServiceImpl : MonoBehaviour, IPartMessageService
    {
        public ICurrentEventInfo CurrentEventInfo
        {
            get {
                if (CurrentEventInfoImpl.current == null)
                    throw new InvalidOperationException("Cannot retrieve source info as not currently in invocation.");

                return CurrentEventInfoImpl.current;
            }
        }

        #region Registration
        /// <summary>
        /// Scan an object for message events and message listeners and hook them up.
        /// Note that all references are dumped on game scene change, so objects must be rescanned when reloaded.
        /// </summary>
        /// <param name="obj">the object to scan</param>
        public void Register<T>(T obj)
        {
            Type objType = typeof(T);

            foreach (MethodInfo meth in objType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                try
                {
                    foreach (object attr in meth.GetCustomAttributes(true))
                        if (attr.GetType().FullName == typeof (PartMessageListener).FullName)
                            AddListener(obj, meth, AsListener(attr));
                }
                catch
                {
                    Debug.LogError("Exception when attempting to register listener: " + (meth.DeclaringType != null ? meth.DeclaringType.AssemblyQualifiedName : "" ) + "." + meth.Name);
                    throw;
                }
            }

            foreach (EventInfo evt in objType.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                bool async;
                try
                {
                    foreach (object attr in evt.GetCustomAttributes(true))
                        if (attr.GetType().FullName == typeof(PartMessageEvent).FullName)
                        {
                            async = AsEvent(attr).IsAsync;
                            goto foundEvent;
                        }                    
                }
                catch
                {
                    Debug.LogError("Exception when attempting to register event: " + (evt.DeclaringType != null ? evt.DeclaringType.AssemblyQualifiedName : "" ) + "." + evt.Name);
                    throw;
                }

                continue;

            foundEvent:
                Type deleg = evt.EventHandlerType;

                // sanity check
                foreach (object attr in deleg.GetCustomAttributes(true))
                    if (attr.GetType().FullName == typeof(PartMessageDelegate).FullName)
                    {
                        goto checkedDelegate;
                    }

                Debug.LogWarning(string.Format("[PartMessageService] Event: {0} in class: {1} declares an event with a part message, but does not have the PartMessageEvent attribute. Will ignore", evt.Name, objType.FullName));
                continue;

            checkedDelegate:
                GenerateEventHandoff(async, obj, evt);
            }
        }

        internal static IPartMessageEventV1 AsEvent(object attr)
        {
            return DuckTyping.Cast<IPartMessageEventV1>(attr);
        }

        internal static IPartMessageListenerV1 AsListener(object attr) 
        {
            return DuckTyping.Cast<IPartMessageListenerV1>(attr);
        }

        internal static IPartMessageDelegateV1 AsDelegate(Attribute attribute)
        {
            return DuckTyping.Cast<IPartMessageDelegateV1>(attribute);
        }

        #endregion

        #region Listeners and Event Delegates

        private Dictionary<string, LinkedList<ListenerInfo>> listeners = new Dictionary<string, LinkedList<ListenerInfo>>();

        private class ListenerInfo
        {
            public WeakReference targetRef;
            public MethodInfo method;
            public IPartMessageListenerV1 attr;

            public object Target
            {
                get
                {
                    return targetRef.Target;
                }
            }

            private Part TargetPart
            {
                get
                {
                    object target = Target;
                    return AsPart(target);
                }
            }

            public bool CheckPrereq(ICurrentEventInfo info)
            {
                if (!attr.Scenes.IsLoaded())
                    return false;
                // ReSharper disable once InvokeAsExtensionMethod SourcePart can be null
                if (!PartUtils.RelationTest(info.SourcePart, TargetPart, attr.Relations))
                    return false;
                return true;
            }
        }

        private void AddListener(object target, MethodInfo meth, IPartMessageListenerV1 attr)
        {
            if (!attr.Scenes.IsLoaded())
                return;

            if (Delegate.CreateDelegate(attr.DelegateType, target, meth, false) == null)
            {
                Debug.LogError(string.Format("PartMessageListener method {0}.{1} does not support the delegate type {2} as declared in the attribute", meth.DeclaringType, meth.Name, attr.DelegateType.FullName));
                return;
            }

            string message = attr.DelegateType.FullName;

            LinkedList<ListenerInfo> listenerList;
            if (!listeners.TryGetValue(message, out listenerList))
            {
                listenerList = new LinkedList<ListenerInfo>();
                listeners.Add(message, listenerList);
            }

            listenerList.AddLast(new ListenerInfo
            {
                targetRef = new WeakReference(target),
                method = meth,
                attr = attr
            });
        }

        private static readonly MethodInfo HandoffSend = typeof(ServiceImpl).GetMethod("SendProxy", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type), typeof(object), typeof(Part), typeof(object[]) }, null);
        private static readonly MethodInfo HandoffSendAsync = typeof(ServiceImpl).GetMethod("SendAsyncProxy", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type), typeof(object), typeof(Part), typeof(object[]) }, null);

        private void GenerateEventHandoff(bool async, object source, EventInfo evt)
        {
            Part part = AsPart(source);

            // This generates a dynamic method that pulls the properties of the event
            // plus the arguments passed and hands it off to the EventHandler method below.
            Type message = evt.EventHandlerType;
            MethodInfo m = message.GetMethod("Invoke", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);


            ParameterInfo[] pLst = m.GetParameters();
            ParameterExpression[] peLst = new ParameterExpression[pLst.Length];
            Expression[] cvrt = new Expression[pLst.Length];
            for (int i = 0; i < pLst.Length; i++)
            {
                peLst[i] = Expression.Parameter(pLst[i].ParameterType, pLst[i].Name);
                cvrt[i] = Expression.Convert(peLst[i], typeof(object));
            }
            Expression createArr = Expression.NewArrayInit(typeof(object), cvrt);

            Expression invoke = Expression.Call(Expression.Constant(this), async?HandoffSendAsync:HandoffSend,
                Expression.Constant(message), Expression.Constant(source), Expression.Constant(part), createArr);

            Delegate d = Expression.Lambda(message, invoke, peLst).Compile();

            // Shouldn't need to use a weak delegate here.
            evt.AddEventHandler(source, d);
        }

        #endregion

        #region Message delivery

        private readonly LinkedList<CurrentEventInfoImpl> asyncMessages = new LinkedList<CurrentEventInfoImpl>();

        public void Send<T>(object source, params object[] args)
        {
            SendProxy(typeof(T), source, AsPart(source), args);
        }

        public void Send(Type message, object source, params object[] args)
        {
            SendProxy(message, source, AsPart(source), args);
        }

        public void SendProxy<T>(object source, Part part, params object[] args)
        {
            SendProxy(typeof(T), source, part, args);
        }

        public void SendProxy(Type messageCls, object source, Part part, params object[] args)
        {
            IPartMessage message = AsIPartMessage(messageCls);
            CurrentEventInfoImpl info = new CurrentEventInfoImpl(message, source, part, args);
            Send(info);
        }

        public void SendAsync<T>(object source, params object[] args)
        {
            SendAsyncProxy(typeof(T), source, AsPart(source), args);
        }

        public void SendAsync(Type message, object source, params object[] args)
        {
            SendAsyncProxy(message, source, AsPart(source), args);
        }

        public void SendAsyncProxy<T>(object source, Part part, params object[] args)
        {
            SendAsyncProxy(typeof(T), source, part, args);
        }

        public void SendAsyncProxy(Type messageCls, object source, Part part, params object[] args) 
        {
            CurrentEventInfoImpl message = new CurrentEventInfoImpl(AsIPartMessage(messageCls), source, part, args);

            // Eat duplicate messages, just send the last one.
            var node = asyncMessages.First;
            while (node != null)
            {
                if (node.Value.Equals(message))
                {
                    var delete = node;
                    node = node.Next;
                    asyncMessages.Remove(delete);
                }
                else
                    node = node.Next;
            }

            using (message.Push())
            {
                if (filters != null && filters.Any(info => info.CheckPrereq(message) && info.Filter(message)))
                    return;
            }
            message.filterComplete = true;

            asyncMessages.AddLast(message);
        }

        public void Update()
        {
            while (asyncMessages.Count > 0)
            {
                CurrentEventInfoImpl message = asyncMessages.First.Value;
                asyncMessages.RemoveFirst();
                try
                {
                    Send(message);
                }
                catch(Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            if (HighLogic.LoadedSceneIsEditor)
                UpdatePartHeirachy();
        }

        internal void Send(CurrentEventInfoImpl message)
        {
            if (!gameObject)
                return;

            using (message.Push())
            {
                if (!message.filterComplete && filters != null)
                    if (filters.Any(info => info.CheckPrereq(message) && info.Filter(message)))
                        return;

                // Send the message
                foreach (IPartMessage currMessage in message.Message)
                {
                    string messageName = currMessage.Name;

                    LinkedList<ListenerInfo> listenerList;
                    if (!listeners.TryGetValue(messageName, out listenerList))
                        continue;

                    // Shorten parameter list if required
                    object[] newArgs = null;

                    for (var node = listenerList.First; node != null; )
                    {
                        // hold reference for duration of call
                        ListenerInfo info = node.Value;
                        object target = info.Target;
                        if (target == null)
                        {
                            // Remove dead links from the list
                            var tmp = node;
                            node = node.Next;
                            listenerList.Remove(tmp);
                            continue;
                        }

                        // Declarative event filtering
                        if (!info.CheckPrereq(message))
                        {
                            node = node.Next;
                            continue;
                        }


                        if (newArgs == null)
                            newArgs = ShortenArgs(message.Arguments, currMessage.DelegateType);

                        try
                        {
                            node.Value.method.Invoke(target, newArgs);
                        }
                        catch (TargetException ex)
                        {
                            // Swallow target exceptions, but not anything else.
                            Debug.LogError(string.Format("Invoking {0}.{1} to handle DelegateType {2} resulted in an exception.", target.GetType(), node.Value.method, CurrentEventInfo.Message));
                            Debug.LogException(ex.InnerException);
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

        #region Message Filters

        // Store the list of current filters in a thread static
        [ThreadStatic]
        private static LinkedList<FilterInfo> filters;

        /// <summary>
        /// Register a message filter. This delegate will be called for every message sent from the source.
        /// If it returns true, the message is considered handled and no futher processing will occour.
        /// </summary>
        /// <param name="filter">The delegate for the filter</param>
        /// <param name="source">Message source, must match. If null will match all sources.</param>
        /// <param name="part">Part to filter. If null will match all parts.</param>
        /// <param name="messages">Optional list of messages to match. If empty, all messages are matched.</param>
        /// <returns>Disposable object. When done call dispose. Works well with using clauses.</returns>
        public IDisposable Filter(PartMessageFilter filter, object source = null, Part part = null, params Type[] messages)
        {
            FilterInfo info = new FilterInfo
            {
                Filter = filter
            };

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
        public IDisposable Consolidate(object source = null, Part part = null, params Type[] messages)
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
        public IDisposable Ignore(object source = null, Part part = null, params Type[] messages)
        {
            return Filter(message => true, source, part, messages);
        }

        private void RegisterFilterInfo(object source, Part part, Type[] messages, FilterInfo info)
        {
            info.source = source;
            info.part = part;
            info.service = this;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < messages.Length; i++)
                info.messages.Add(messages[i].FullName);

            if(filters == null)
                filters = new LinkedList<FilterInfo>();

            info.node = filters.AddFirst(info);
        }

        internal class FilterInfo : IDisposable
        {
            public object source;
            public Part part;
            public PartMessageFilter Filter;
            public HashSet<string> messages = new HashSet<string>();

            public ServiceImpl service;

            public LinkedListNode<FilterInfo> node;

            public bool CheckPrereq(ICurrentEventInfo info)
            {
                if (source != null && source != info.Source)
                    return false;
                if (part != null && part != info.SourcePart)
                    return false;
                if (messages.Count == 0)
                    return true;

                return info.Message.Any(message => !messages.Contains(message.Name));
            }

            public virtual void Dispose()
            {
                if(service == null)
                    throw new InvalidOperationException("Already disposed");

                filters.Remove(node);
                if (filters.Count == 0)
                    filters = null;
                service = null;
            }

            ~FilterInfo()
            {
                if (service == null)
                    return;
                Dispose();
                Debug.LogError("Warning: Filter has been created and not disposed prior to finalization. Please check the code.");
            }
        }

        internal class MessageConsolidator : FilterInfo
        {
            public MessageConsolidator()
            {
                Filter = ConsolidatingFilter;
            }

            private LinkedList<ICurrentEventInfo> messageList = new LinkedList<ICurrentEventInfo>();

            private bool ConsolidatingFilter(ICurrentEventInfo message)
            {
                // Remove any matching previous
                messageList.RemoveAll(evt => evt.Equals(message));
                messageList.AddLast(message);
                return true;
            }

            public override void Dispose()
            {
                ServiceImpl theService = service;
                base.Dispose();

                // Safe as we've already deregistered the filter, so no loops.
                foreach (ICurrentEventInfo message in messageList)
                {
                    CurrentEventInfoImpl info = (CurrentEventInfoImpl)message;
                    info.filterComplete = false;
                    theService.Send((CurrentEventInfoImpl)message);
                }
                messageList = null;
            }

        }
        #endregion

        #region Startup

        internal void Awake() 
        {
            // Clear the listeners list when reloaded.
            GameEvents.onGameSceneLoadRequested.Add(SceneLoadedListener);
            GameEvents.onInputLocksModified.Add(OnInputLocksModified);
            GameEvents.onPartAttach.Add(OnPartAttach);
            GameEvents.onPartRemove.Add(OnPartRemove);
        }

        internal void OnDestroy()
        {
            GameEvents.onGameSceneLoadRequested.Remove(SceneLoadedListener);
            GameEvents.onInputLocksModified.Remove(OnInputLocksModified);
            GameEvents.onPartAttach.Remove(OnPartAttach);
            GameEvents.onPartRemove.Remove(OnPartRemove);
            listeners = null;
        }

        private void SceneLoadedListener(GameScenes scene)
        {
            currRoot = null;
            listeners.Clear();
        }

        private Part currRoot;

        private void OnInputLocksModified(GameEvents.FromToAction<ControlTypes, ControlTypes> data)
        {
            if (!HighLogic.LoadedSceneIsEditor)
                return;

            if (EditorLogic.fetch == null)
                return;

            var ship = EditorLogic.fetch.ship;

            if (ship == null)
                return;

            if (ship.parts.Count > 0)
            {
                if (!ReferenceEquals(ship.parts[0], currRoot))
                {
                    Part root = ship.parts[0];
                    CheckKnown(root);
                    SendAsyncProxy<PartRootSelected>(this, root);
                    currRoot = root;
                }
            }
            else if(currRoot != null)
            {
                SendAsyncProxy<PartRootRemoved>(this, currRoot);
                currRoot = null;
            }
        }

        private Part lastSelected;
       
        private void UpdatePartHeirachy()
        {
            Part selectedPart = EditorLogic.SelectedPart;
            if (ReferenceEquals(lastSelected, selectedPart)
                || (lastSelected = selectedPart) == null
                || CheckKnown(selectedPart))
                return;

            // Parts that are clones (Alt-Click) won't have had their attach methods called.
            PartAttachSymmetry(selectedPart);
        }

        private bool CheckKnown(Part part)
        {
            if (!part.isClone)
                return true;

            KnownPartMarker marker = part.gameObject.GetTaggedComponent<KnownPartMarker>();
            if (marker == null)
            {
                marker = part.gameObject.AddTaggedComponent<KnownPartMarker>();
                marker.known = true;
                return false;
            }

            bool ret = marker.known;
            marker.known = true;
            return ret;
        }

        private void OnPartAttach(GameEvents.HostTargetAction<Part, Part> data)
        {
            // Target is the parent, host is the child part
            SendAsyncProxy<PartParentChanged>(this, data.host, data.target);
            SendAsyncProxy<PartChildAttached>(this, data.target, data.host);

            if (CheckKnown(data.host))
                return;

            // Symmetry clones won't have had either of the above listeners called for any of their children
            PartAttachSymmetry(data.host);
        }

        private void PartAttachSymmetry(Part thisPart)
        {
            foreach (Part child in thisPart.children)
            {
                SendAsyncProxy<PartParentChanged>(this, child, thisPart);
                SendAsyncProxy<PartChildAttached>(this, thisPart, child);
                PartAttachSymmetry(child);
            }
        }

        private void OnPartRemove(GameEvents.HostTargetAction<Part, Part> data)
        {
            // host is null, target is the child part. 
            SendAsyncProxy<PartParentChanged>(this, data.target, new object[] { null });
            SendAsyncProxy<PartChildDetached>(this, data.target.parent, data.target);

            if (data.target.attachMode == AttachModes.SRF_ATTACH)
                data.target.srfAttachNode.attachedPart = null;

        }

        #endregion

        #region Conversion to IPartMessage
        private readonly Dictionary<Type, IPartMessage> cachedPartMessages = new Dictionary<Type, IPartMessage>();

        /// <summary>
        /// Convert delegate type into the IPartMessage interface.
        /// </summary>
        /// <param name="type">Delegate type to convert. This must be a delegate type marked with the <see cref="PartMessageDelegate"/> attribute.</param>
        public IPartMessage AsIPartMessage(Type type)
        {
            IPartMessage value;
            if (cachedPartMessages.TryGetValue(type, out value))
                return value;
            return cachedPartMessages[type] = new MessageImpl(this, type);
        }

        /// <summary>
        /// Convert delegate type into the IPartMessage interface.
        /// </summary>
        /// <typeparam name="T">Delegate type to convert. This must be a delegate type marked with the <see cref="PartMessageDelegate"/> attribute.</typeparam>
        public IPartMessage AsIPartMessage<T>()
        {
            return AsIPartMessage(typeof(T));
        }
        #endregion

        #region Utility Functions
        private static Part AsPart(object src) 
        {
            Part part = src as Part;
            if (part != null)
                return part;
            PartModule module = src as PartModule;
            if (module != null)
                return module.part;

            if(src.GetType().GetInterfaces().Any(t => t.FullName == typeof (IPartMessagePartProxy).FullName || t.FullName == "KSPAPIExtensions.PartMessagePartProxy"))
                return DuckTyping.Cast<IPartMessagePartProxy>(src).ProxyPart;
            return null;
        }
        #endregion
    }

    #region Initialization and Other Mod Interfacing
    internal class PartMessageServiceInitializer : MonoBehaviour
    {
        private static bool loadedInScene;

        internal void Awake()
        {
            // Ensure that only one copy of the service is run per scene change.
            if (loadedInScene)
            {
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                Debug.Log("[PartMessageService] Multiple copies of current version. Using the first copy. Version: " + currentAssembly.GetName().Version);
                Destroy(gameObject);
                return;
            }
            loadedInScene = true;
            
            if (!SystemUtils.RunTypeElection(typeof(PartMessageService), "KSPAPIExtensions"))
                return;

            // So at this point we know we have won the election, and will be using the class versions as in this assembly.

            // Destroy the old service
            if (PartMessageService._instance != null)
            {
                Debug.Log("[PartMessageService] destroying service from previous load");
                Destroy(((ServiceImpl)PartMessageService._instance).gameObject);
            }

            // Create the part message service
            GameObject serviceGo = new GameObject(PartMessageService.PartMessageServiceName);
            DontDestroyOnLoad(serviceGo);

            // Assign the service to the static variable
            PartMessageService._instance = serviceGo.AddTaggedComponent<ServiceImpl>();

            // At this point the losers will duck-type themselves to the latest version of the service if they're called.

            ListenerFerramAerospaceResearch.AddListener(serviceGo);
        }

        public void Update()
        {
            loadedInScene = false;
            Destroy(gameObject);
        }
    }

    internal class KnownPartMarker : MonoBehaviour
    {
        [NonSerialized]
        internal bool known;
    }

    internal class ListenerFerramAerospaceResearch : MonoBehaviour
    {
        // ReSharper disable once InconsistentNaming
        private static Action<Part> SetBasicDragModuleProperties;

        public static void AddListener(GameObject serviceGo)
        {
            Type typeFARAeroUtil = Type.GetType("ferram4.FARAeroUtil", false);
            if (typeFARAeroUtil == null)
                return;

            MethodInfo info = typeFARAeroUtil.GetMethod("SetBasicDragModuleProperties", new[] { typeof(Part) });

            if (info == null)
            {
                Debug.LogWarning("[PartMessageService] FAR update method seems to have changed. Cannot interface with FAR.");
                return;
            }

            SetBasicDragModuleProperties = (Action<Part>)Delegate.CreateDelegate(typeFARAeroUtil, info);

            serviceGo.AddTaggedComponent<ListenerFerramAerospaceResearch>();
        }

        private IPartMessageService service;

        public void Awake()
        {
            service = PartMessageService.Instance;
            GameEvents.onGameSceneLoadRequested.Add(GameSceneLoaded);
        }

        private void GameSceneLoaded(GameScenes data)
        {
            if(!GameSceneFilter.AnyEditorOrFlight.IsLoaded())
                return;

            PartMessageService.Register(this);
        }

        [PartMessageListener(typeof(PartModelChanged), relations:PartRelationship.Unknown, scenes: GameSceneFilter.AnyEditorOrFlight)]
        public void PartModelChanged()
        {
            Part part = service.CurrentEventInfo.SourcePart;
            if (part == null)
                return;

            SetBasicDragModuleProperties(part);
        }
    }


    #endregion

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
                current = this.top = top;
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
                        PartMessageDelegate evt = (PartMessageDelegate)current.GetCustomAttributes(typeof(PartMessageDelegate), true)[0];
                        current = evt.Parent;
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
