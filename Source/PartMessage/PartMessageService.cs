using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using System.Linq.Expressions;
using System.Collections;
using System.Text.RegularExpressions;

namespace KSPAPIExtensions.PartMessage
{
    /// <summary>
    /// PartMessageListeners can use the properties in this class to examine details about the current message being
    /// handled
    /// </summary>
    public interface CurrentMessageInfo
    {
        /// <summary>
        /// The message type
        /// </summary>
        Type message
        {
            get;
        }

        /// <summary>
        /// All messages this represents, from most specific to most general
        /// </summary>
        IEnumerable<Type> allMessages
        {
            get;
        }

        /// <summary>
        /// The source of the event
        /// </summary>
        object source
        {
            get;
        }

        /// <summary>
        /// The source part. This may be null if the source was not a Part or PartModule
        /// </summary>
        Part part
        {
            get;
        }

        /// <summary>
        /// The source PartModule. This will be null if the source was not a PartModule
        /// </summary>
        PartModule srcModule
        {
            get;
        }

        /// <summary>
        /// Find relationship between the message source and the specified part.
        /// </summary>
        /// <param name="destPart"></param>
        /// <returns>The relationship. This will be PartRelationship.Unknown if the dest part is null.</returns>
        PartRelationship SourceRelationTo(Part destPart);
    }

    public interface PartMessageService
    {

        /// <summary>
        /// Get the source info, an interface used to get information about the current message source.
        /// </summary>
        CurrentMessageInfo SourceInfo { get; }

        #region Object scanning
        /// <summary>
        /// Scan an object for message events and message listeners and hook them up.
        /// This is generally called in the constructor for the object.
        /// </summary>
        /// <param name="obj">the object to scan</param>
        void ScanObject<T>(T obj);
        #endregion

        #region Message Sending and Filtering
        /// <summary>
        /// Send a message. Normally this will be automatically invoked by the event, but there are types when dynamic invocation is required.
        /// </summary>
        /// <param name="source">Source of the message. This should be either a Part or a PartModule.</param>
        /// <param name="message">The message delegate type. This must have the PartMessage attribute.</param>
        /// <param name="args">message arguments</param>
        void SendPartMessage(object source, Type message, params object[] args);

        /// <summary>
        /// Send a message. Normally this will be automatically invoked by the event, but there are types when dynamic invocation is required.
        /// This version allows the source to proxy for some other part.
        /// </summary>
        /// <param name="source">Source of the message. This may be any object</param>
        /// <param name="part">Part that the message source is proxying for</param>
        /// <param name="message">The message delegate type. This must have the PartMessage attribute.</param>
        /// <param name="args">message arguments</param>
        void SendPartMessage(object source, Part part, Type message, params object[] args);

        /// <summary>
        /// Register a message filter. This delegate will be called for every message sent from the source.
        /// If it returns true, the message is considered handled and no futher processing will occour.
        /// </summary>
        /// <param name="filter">The delegate for the filter</param>
        /// <param name="source">Message source, must match. If null will match all sources.</param>
        /// <param name="messages">Optional list of messages to match. If empty, all messages are matched.</param>
        /// <returns>Disposable object. When done call dispose. Works well with using clauses.</returns>
        IDisposable MessageFilter(PartMessageFilter filter, object source = null, Part part = null, params Type[] messages);

        /// <summary>
        /// Consolidate messages. All messages sent by the source will be held until the returned object is destroyed.
        /// Any duplicates of the same message and same arguments will be swallowed silently.
        /// </summary>
        /// <param name="source">source to consolidate from. Null will match all sources</param>
        /// <param name="messages">messages to consolidate. If not specified, all messages are consolidated.</param>
        /// <returns>Disposable object. When done call dispose. Works well with using clauses.</returns>
        IDisposable MessageConsolidate(object source = null, Part part = null, params Type[] messages);

        /// <summary>
        /// Ignore messages sent by the source until the returned object is destroyed.
        /// </summary>
        /// <param name="source">Source to ignore. Null will ignore all sources.</param>
        /// <param name="messages">Messages to ignore. If not specified, all messages are ignored.</param>
        /// <returns>Disposable object. When done call dispose. Works well with using clauses.</returns>
        IDisposable MessageIgnore(object source = null, Part part = null, params Type[] messages);
        #endregion
    }


    [KSPAddonFixed(KSPAddon.Startup.Instantly, false, typeof(PartMessageFinder))]
    public sealed class PartMessageFinder : MonoBehaviour
    {
        public static PartMessageService Service
        {
            get;
            private set;
        }

        public static CurrentMessageInfo SourceInfo
        {
            get { return Service.SourceInfo; }
        }

        public static void Register<T>(T obj) 
        {
            Service.ScanObject<T>(obj);
        }

        private PartMessageFinder() { }

        internal void Update()
        {
            // Use the Update method to be sure this runs *after* module manager. (MM used OnGUI)

            // If we're not ready to run, return and wait for the next Update
            if (!GameDatabase.Instance.IsReady() && !GameSceneFilter.AnyInitializing.IsLoaded())
                return;

            // If we are loaded from the first loaded assembly that has this class, then we are responsible to destroy
            var candidates = (from ass in AssemblyLoader.loadedAssemblies
                             where ass.assembly.GetType(typeof(PartMessageService).FullName, false) != null
                             orderby ass.assembly.GetName().Version descending, ass.path ascending
                             select ass).ToArray();
            var winner = candidates.First();

            if (Assembly.GetExecutingAssembly() != winner.assembly)
            {
                // We are not the winner, return.
                UnityEngine.Object.Destroy(gameObject);
                return;
            }
            
            if (candidates.Length > 1)
            {
                string losers = string.Join("\n", (from t in candidates
                                                   where t != winner
                                                   select string.Format("Version: {0} Location: {1}", t.assembly.GetName().Version, t.path)).ToArray());

                Debug.Log("[PartMessageService] version " + winner.assembly.GetName().Version + " at " + winner.path + " won the election against\n" + losers);
            }
            else
                Debug.Log("[PartMessageService] Elected unopposed version= " +  winner.assembly.GetName().Version + " at " + winner.path);

            // Destroy the old service
            if (Service != null)
            {
                Debug.Log("[PartMessageService] destroying service from previous load");
                UnityEngine.Object.Destroy(((ServiceImpl)Service).gameObject);
                Service = null;
            }

            // Create the part message service
            GameObject serviceGo = new GameObject("PartMessageService");
            UnityEngine.Object.DontDestroyOnLoad(serviceGo);

            // Create the master (winner which is ourselves)
            Service = serviceGo.AddComponent<ServiceImpl>();

            // Add the old versions
            object prevService = Service;
            object master = Service;
            List<object> otherVersions = new List<object>();

            foreach (var ass in candidates)
            {
                if (ass == winner)
                    continue;
                // We may need to do some version dependent init, not currently

                // Find the types we need
                Type serviceCls = ass.assembly.GetType(typeof(ServiceImpl).FullName, false);
                Type finderCls = ass.assembly.GetType(typeof(PartMessageFinder).FullName, false);
                if (serviceCls == null || finderCls == null)
                {
                    Debug.LogError("[PartMessageService] Unable to find old version required classes: version=" + ass.assembly.GetName().Version + " at " + ass.path);
                    continue;
                }

                // Instantiate the service. They can coalesce if the types are equal, but set the static field in all the finder classes.
                object service;
                if (prevService.GetType() == serviceCls)
                    service = prevService;
                else
                    service = serviceGo.AddComponent(serviceCls);

                // Set the service in the finder
                MethodInfo serviceProp = finderCls.GetProperty("Service").GetSetMethod(true);
                serviceProp.Invoke(null, new object[] { service });
            }

            // ensure all versions are set up
            ((ServiceImpl)master).SetMaster(master, otherVersions);
            object [] args = new object[] { master, otherVersions};
            foreach (object other in otherVersions)
            {
                Type type = other.GetType();
                type.InvokeMember("SetMaster", BindingFlags.Instance | BindingFlags.NonPublic, null, other, args);
            }

            // Destroy ourself because there's no reason to still hang around
            UnityEngine.Object.Destroy(gameObject);
        }
    }
}
