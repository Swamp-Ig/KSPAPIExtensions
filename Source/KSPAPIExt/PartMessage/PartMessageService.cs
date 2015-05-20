using System;
using System.Collections.Generic;
using UnityEngine;
using DeftTech.DuckTyping;

namespace KSPAPIExtensions.PartMessage
{
    /// <summary>
    /// Interface for a part message once it is passed into the system.
    /// </summary>
    public interface IPartMessage : IEnumerable<IPartMessage>
    {
        /// <summary>
        /// String name of the part message. This will be equal to the FullName attibute of the delegate type.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Delegate type of the message. <b>Note: do not rely on type equality with this attribute</b>. This is because
        /// the source may be using a different assembly to the target.
        /// </summary>
        Type DelegateType { get; }

        /// <summary>
        /// Often there is a heirachy of events - with more specific events and encompasing general events.
        /// Define a general event as the parent in this instance and any listeners to the general event
        /// will also be notified. Note that the arguments in this situation are expected to be a truncation
        /// of the argument list for this event.
        /// </summary>
        IPartMessage Parent { get; }

        /// <summary>
        /// This event is considered abstract - it should not be sent directly but should be sent from one of the child events.
        /// </summary>
        bool IsAbstract { get; }
    }

    /// <summary>
    /// PartMessageListeners can use the properties in this class to examine details about the current message being
    /// handled
    /// </summary>
    public interface ICurrentEventInfo : IEquatable<ICurrentEventInfo>
    {
        /// <summary>
        /// The message
        /// </summary>
        IPartMessage Message { get; }

        /// <summary>
        /// The source of the event
        /// </summary>
        object Source { get; }

        /// <summary>
        /// The source part. This may be null if the source was not a Part or PartModule
        /// </summary>
        Part SourcePart { get; }

        /// <summary>
        /// The source PartModule. This will be null if the source was not a PartModule
        /// </summary>
        PartModule SourceModule { get; }

        /// <summary>
        /// The arguments to the current event. Treat as unmodifiable.
        /// </summary>
        object[] Arguments { get; }

        /// <summary>
        /// The arguments that are used when determining event equality. This is any arguments not explicitly marked with <see cref="UseLatest"/>
        /// </summary>
        IEnumerable<object> IdentArguments { get; }

        /// <summary>
        /// Find relationship between the message source and the specified part.
        /// </summary>
        /// <returns>The relationship. This will be PartRelationship.Unknown if the dest part is null or the source part is unknown.</returns>
        PartRelationship SourceRelationTo(Part destPart);
    }

    public interface IPartMessageService
    {
        /// <summary>
        /// CurrentEventInfo for the current message being processed. This is used to get information about the current message source.
        /// </summary>
        ICurrentEventInfo CurrentEventInfo { get; }

        /// <summary>
        /// Scan an object for events marked with <see cref="PartMessageEvent"/> and methods marked with <see cref="PartMessageListener"/> and hook them up.
        /// This is generally called either in the constructor, or in OnAwake for Part and PartModules.
        /// Note that this method does <b>not</b> scan base classes for events and listeners, they need to be scanned explicitly.
        /// </summary>
        /// <param name="obj">Object to register. If this is a Part, a PartModule, or a IPartMessagePartProxy the recieving part will be discovered.</param>
        void Register<T>(T obj);

        /// <summary>
        /// Send a message. Normally this will be automatically invoked by the event, but there are types when dynamic invocation is required.
        /// </summary>
        /// <typeparam name="T">Message type. This must be a delegate type marked with the PartMessageDelegate attribute.</typeparam>
        /// <param name="source">Source of the message. If this is a Part, a PartModule, or a IPartMessagePartProxy the source part will be discovered.</param>
        /// <param name="args">message arguments</param>
        void Send<T>(object source, params object[] args);

        /// <summary>
        /// Send a message. Normally this will be automatically invoked by the event, but there are types when dynamic invocation is required.
        /// </summary>
        /// <param name="message">The message delegate type. This must have the PartMessageDelegate attribute.</param>
        /// <param name="source">Source of the message. If this is a Part, a PartModule, or a IPartMessagePartProxy the source part will be discovered.</param>
        /// <param name="args">message arguments</param>
        void Send(Type message, object source, params object[] args);

        /// <summary>
        /// Send a message. Normally this will be automatically invoked by the event, but there are types when dynamic invocation is required.
        /// This version allows the source to proxy for some other part.
        /// </summary>
        /// <typeparam name="T">Message type. This must be a delegate type marked with the PartMessageDelegate attribute.</typeparam>
        /// <param name="source">Source of the message. This may be any object. This variant does <b>not</b> do automatic part discovery.</param>
        /// <param name="part">Part that the message source is proxying for</param>
        /// <param name="args">message arguments</param>
        void SendProxy<T>(object source, Part part, params object[] args);

        /// <summary>
        /// Send a message. Normally this will be automatically invoked by the event, but there are types when dynamic invocation is required.
        /// This version allows the source to proxy for some other part.
        /// </summary>
        /// <param name="message">The message delegate type. This must have the PartMessageDelegate attribute.</param>
        /// <param name="source">Source of the message. This may be any object. This variant does <b>not</b> do automatic part discovery.</param>
        /// <param name="part">Part that the message source is proxying for</param>
        /// <param name="args">message arguments</param>
        void SendProxy(Type message, object source, Part part, params object[] args);

        /// <summary>
        /// Send a message. Normally this will be automatically invoked by the event, but there are types when dynamic invocation is required.
        /// This version of the method will send the message asynchonously - the message will be delivered in the next update frame. Any message filters will
        /// be invoked prior to returning.
        /// </summary>
        /// <typeparam name="T">Message type. This must be a delegate type marked with the PartMessageDelegate attribute.</typeparam>
        /// <param name="source">Source of the message. If this is a Part, a PartModule, or a IPartMessagePartProxy the source part will be discovered.</param>
        /// <param name="args">message arguments</param>
        void SendAsync<T>(object source, params object[] args);

        /// <summary>
        /// Send a message. Normally this will be automatically invoked by the event, but there are types when dynamic invocation is required.
        /// This version of the method will send the message asynchonously - the message will be delivered in the next update frame. Any message filters will
        /// be invoked prior to returning.
        /// </summary>
        /// <param name="message">The message delegate type. This must have the PartMessageDelegate attribute.</param>
        /// <param name="source">Source of the message. If this is a Part, a PartModule, or a IPartMessagePartProxy the source part will be discovered.</param>
        /// <param name="args">message arguments</param>
        void SendAsync(Type message, object source, params object[] args);

        /// <summary>
        /// Send a message. Normally this will be automatically invoked by the event, but there are types when dynamic invocation is required.
        /// This version of the method will send the message asynchonously - the message will be delivered in the next update frame. Any message filters will
        /// be invoked prior to returning.
        /// </summary>
        /// <typeparam name="T">Message type. This must be a delegate type marked with the PartMessageDelegate attribute.</typeparam>
        /// <param name="source">Source of the message. This may be any object. This variant does <b>not</b> do automatic part discovery.</param>
        /// <param name="part">Part that the message source is proxying for</param>
        /// <param name="args">message arguments</param>
        void SendAsyncProxy<T>(object source, Part part, params object[] args);

        /// <summary>
        /// Send a message. Normally this will be automatically invoked by the event, but there are types when dynamic invocation is required.
        /// This version of the method will send the message asynchonously - the message will be delivered in the next update frame. Any message filters will
        /// be invoked prior to returning.
        /// </summary>
        /// <param name="message">The message delegate type. This must have the PartMessageDelegate attribute.</param>
        /// <param name="source">Source of the message. This may be any object. This variant does <b>not</b> do automatic part discovery.</param>
        /// <param name="part">Part that the message source is proxying for</param>
        /// <param name="args">message arguments</param>
        void SendAsyncProxy(Type message, object source, Part part, params object[] args);

        /// <summary>
        /// Register a message filter. This delegate will be called for every message sent from the source.
        /// If it returns true, the message is considered handled and no futher processing will occour.
        /// </summary>
        /// <param name="filter">The delegate for the filter</param>
        /// <param name="source">Message source, must match. If null will match all sources.</param>
        /// <param name="part">Source part to match. If null will match all parts.</param>
        /// <param name="messages">Optional list of messages to match. If empty, all messages are matched.</param>
        /// <returns>Disposable object. When done call dispose. Works well with using clauses.</returns>
        IDisposable Filter(PartMessageFilter filter, object source = null, Part part = null, params Type[] messages);
        
        /// <summary>
        /// Consolidate messages. All messages sent by the source will be held until the returned object is destroyed.
        /// Any duplicates of the same message and same arguments will be swallowed silently.
        /// </summary>
        /// <param name="source">source to consolidate from. Not specified or null will match all sources</param>
        /// <param name="part">Source part to match. If null will match all parts.</param>
        /// <param name="messages">messages to consolidate. If not specified, all messages are consolidated.</param>
        /// <returns>Disposable object. When done call dispose. Works well with using clauses.</returns>
        IDisposable Consolidate(object source = null, Part part = null, params Type[] messages);

        /// <summary>
        /// Ignore messages sent by the source until the returned object is destroyed.
        /// </summary>
        /// <param name="source">Source to ignore. Null will ignore all sources.</param>
        /// <param name="part">Source part to match. If null will match all parts.</param>
        /// <param name="messages">Messages to ignore. If not specified, all messages are ignored.</param>
        /// <returns>Disposable object. When done call dispose. Works well with using clauses.</returns>
        IDisposable Ignore(object source = null, Part part = null, params Type[] messages);

        /// <summary>
        /// Convert delegate type into the IPartMessage interface.
        /// </summary>
        /// <param name="type">Delegate type to convert. This must be a delegate type marked with the <see cref="PartMessageDelegate"/> attribute.</param>
        IPartMessage AsIPartMessage(Type type);

        /// <summary>
        /// Convert delegate type into the IPartMessage interface.
        /// </summary>
        /// <typeparam name="T">Delegate type to convert. This must be a delegate type marked with the <see cref="PartMessageDelegate"/> attribute.</typeparam>
        IPartMessage AsIPartMessage<T>();
    }

    /// <summary>
    /// PartMessageService. This abstract base class primaraly implements the finder for the service (Instance) along with some static short-cut methods.
    /// </summary>
    static public class PartMessageService
    {
        internal static readonly string PartMessageServiceName = typeof(PartMessageService).FullName + ":SingletonInstance";
        // ReSharper disable once InconsistentNaming
        internal static IPartMessageService _instance;

        /// <summary>
        /// Get the instance of the PartMessageService. Note that this may be a duck-typed proxy.
        /// </summary>
        public static IPartMessageService Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                GameObject serviceGo = GameObject.Find(PartMessageServiceName);
                if (serviceGo == null)
                    throw new InvalidProgramException("PartMessageService has not been initialized.");

                foreach (Component comp in serviceGo.GetComponents<Component>())
                {
                    if (comp.GetType().FullName.StartsWith(typeof(ServiceImpl).FullName))
                        return _instance = DuckTyping.Cast<IPartMessageService>(comp);
                }

                throw new InvalidProgramException("Unable to find a compatible part message service from updated assembly. Something has gone very wrong.");
            }
            internal set
            {
                _instance = value;
            }
        }

        /// <summary>
        /// Convenience short-cut method for getting the current CurrentEventInfo. This interface allows message listeners to find information about the sender.
        /// This object can be cached for later use, and will not update in future invocations.
        /// </summary>
        /// <exception cref="InvalidOperationException">If there is no current invocation occouring.</exception>
        public static ICurrentEventInfo MessageInfo
        {
            get { return Instance.CurrentEventInfo; }
        }

        /// <summary>
        /// Convenience short-cut <see cref="IPartMessageService.Register{T}"/>. 
        /// Scan an object for events marked with <see cref="PartMessageEvent"/> and methods marked with <see cref="PartMessageListener"/> and hook them up.
        /// This is generally called either in the constructor, or in OnAwake for Part and PartModules.
        /// Note that this method does <b>not</b> scan base classes for events and listeners, they need to be scanned explicitly.
        /// </summary>
        /// <typeparam name="T">The type of the object to register. This can normally be inferred from the argument type.</typeparam>
        /// <param name="obj">Object to register. If this is a Part, a PartModule, or a IPartMessagePartProxy the recieving part will be discovered.</param>
        public static void Register<T>(T obj) 
        {
            Instance.Register(obj);
        }

        /// <summary>
        /// Convenience short-cut <see cref="IPartMessageService.Send{T}"/>. 
        /// Send a message. Normally this will be automatically invoked by the event, but there are types when dynamic invocation is required.
        /// This version allows the source to proxy for some other part.
        /// </summary>
        /// <typeparam name="T">The delegate type of the message to send. This must be a delegate, and must be marked with <see cref="PartMessageDelegate"/> attribute.</typeparam>
        /// <param name="source">Source of the message. This may be any object. This variant does <b>not</b> do automatic part discovery.</param>
        /// <param name="part">Part that the message source is proxying for</param>
        /// <param name="args">message arguments</param>
        public static void Send<T>(object source, Part part, params object[] args)
        {
            Instance.SendProxy<T>(source, part, args);
        }
    }
}
