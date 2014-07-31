using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace KSPAPIExtensions
{
    public static class SystemUtils
    {

        /// <summary>
        /// Ensure the latest version of a particular type is used. This is useful when multiple versions of the same assembly are potentially loaded.
        /// 
        /// This will return true for the latest version of the class (specified with <see cref="AssemblyVersionAttribute"/>)
        /// that appears the earliest in the list of loaded assemblies (as loaded from <see cref="AssemblyLoader.loadedAssemblies"/>).
        /// </summary>
        /// <param name="targetCls">Target class to search for. Searched by the <see cref="Type.FullName"/> attribute.</param>
        /// <param name="assemName">Assembly name of the expected assembly.</param>
        /// <exception cref="InvalidProgramException">If the class is not in an assembly named <paramref name="assemName"/>. </exception>
        /// <returns>True if this class wins the election, false otherwise.</returns>
        public static bool RunTypeElection(Type targetCls, String assemName)
        {
            if (targetCls.Assembly.GetName().Name != assemName)
                throw new InvalidProgramException("Assembly: " + targetCls.Assembly.GetName().Name + " at location: " + targetCls.Assembly.Location + " is not in the expected assembly. Code has been copied and this will cause problems.");

            // If we are loaded from the first loaded assembly that has this class, then we are responsible to destroy
            var candidates = (from ass in AssemblyLoader.loadedAssemblies
                where ass.assembly.GetName().Name == assemName &&
                      ass.assembly.GetType(targetCls.FullName, false) != null
                orderby ass.assembly.GetName().Version descending, ass.path ascending
                select ass).ToArray();
            var winner = candidates.First();

            if (!ReferenceEquals(targetCls.Assembly, winner.assembly))
                return false;

            if (candidates.Length > 1)
            {
                string losers = string.Join("\n", (from t in candidates
                                                   where t != winner
                                                   select string.Format("Version: {0} Location: {1}", t.assembly.GetName().Version, t.path)).ToArray());

                Debug.Log("[" + targetCls.Name + "] version " + winner.assembly.GetName().Version + " at " + winner.path + " won the election against\n" + losers);
            }
            else
                Debug.Log("[" + targetCls.Name + "] Elected unopposed version= " + winner.assembly.GetName().Version + " at " + winner.path);

            return true;
        }

        /// <summary>
        /// Ensure the latest version of a particular type is used. This is useful when multiple versions of the same assembly are potentially loaded.
        /// 
        /// This will the latest version of the class (specified with <see cref="AssemblyVersionAttribute"/>)
        /// that appears the earliest in the list of loaded assemblies (as loaded from <see cref="AssemblyLoader.loadedAssemblies"/>).
        /// </summary>
        /// <param name="targetCls">Target class to search for. Searched by the <see cref="Type.FullName"/> attribute.</param>
        /// <param name="assemName">Assembly name of the expected assembly.</param>
        /// <exception cref="InvalidProgramException">If the class is not in an assembly named <paramref name="assemName"/>. </exception>
        /// <returns>True if this class wins the election, false otherwise.</returns>
        public static Type TypeElectionWinner(Type targetCls, String assemName)
        {
            if (targetCls.Assembly.GetName().Name != assemName)
                throw new InvalidProgramException("Assembly: " + targetCls.Assembly.GetName().Name + " at location: " +
                                                  targetCls.Assembly.Location +
                                                  " is not in the expected assembly. Code has been copied and this will cause problems.");

            // If we are loaded from the first loaded assembly that has this class, then we are responsible to destroy
            var candidates = (from ass in AssemblyLoader.loadedAssemblies
                where ass.assembly.GetName().Name == assemName
                let t = ass.assembly.GetType(targetCls.FullName, false)
                where t != null
                orderby ass.assembly.GetName().Version descending, ass.path ascending
                select t).ToArray();
            return candidates.First();
        }

        /// <summary>
        /// Find a version-tagged class for an untagged class.
        /// 
        /// The tagged class must be directly derived from the untagged class and in the same assembly and namespace.
        /// </summary>
        /// <param name="baseClass">The untagged class for which the tagged class will be searched. The <see cref="Type.FullName"/> attribute.</param>
        /// <returns>The tagged class if found, otherwise the base class</returns>
        public static Type VersionTaggedType(Type baseClass)
        {
            var ass = baseClass.Assembly;
            Type tagged = ass.GetTypes().Where(t => t.BaseType == baseClass).Where(t => t.FullName.StartsWith(baseClass.FullName)).FirstOrDefault();
            if (tagged != null) {
                Debug.Log(String.Format("[VersionTaggedType] found {0} for {1}", tagged.FullName, baseClass.FullName));
                return tagged;
            }
            return baseClass;
        }

        public static LinkedListNode<T> FindFirstNode<T>(this LinkedList<T> list, Predicate<T> match)
        {
            for (var node = list.First; node != null; node = node.Next)
                if(match(node.Value))
                    return node;
            return null;
        }

        public static int RemoveAll<T>(this LinkedList<T> list, Predicate<T> match)
        {
            int count = 0;
            for (var node = list.First; node != null; )
            {
                if (!match(node.Value))
                {
                    node = node.Next;
                    continue;
                }
                ++count;
                var tmp = node;
                node = node.Next;
                list.Remove(tmp);
            }
            return count;
        }

        // ReSharper disable once InconsistentNaming
        public static bool TryGet<K, T>(this KeyedCollection<K, T> coll, K key, out T value)
        {
            try
            {
                value = coll[key];
                return true;
            }
            catch (KeyNotFoundException)
            {
                value = default(T);
                return false;
            }
        }

        /// <summary>
        /// Retrieve the most informative version string available in the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly from which to retrieve the version string</param>
        /// <returns>The version string from AssemblyInformationalVersionAttribute if available, otherwise from AssemblyVersion.</returns>
        public static string GetAssemblyVersionString (Assembly assembly)
        {
            string version = assembly.GetName().Version.ToString ();

            var cattrs = assembly.GetCustomAttributes(true);
            foreach (var attr in cattrs) {
                if (attr is AssemblyInformationalVersionAttribute) {
                    var ver = attr as AssemblyInformationalVersionAttribute;
                    version = ver.InformationalVersion;
                    break;
                }
            }

            return version;
        }

        /// <summary>
        /// Retrieve the best available title string available in the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly from which to retrieve the title string</param>
        /// <returns>The title string from AssemblyTitle if available, otherwise the name of the dll.</returns>
        public static string GetAssemblyTitle (Assembly assembly)
        {
            string title = assembly.GetName().Name;

            var cattrs = assembly.GetCustomAttributes(true);
            foreach (var attr in cattrs) {
                if (attr is AssemblyTitleAttribute) {
                    var ver = attr as AssemblyTitleAttribute;
                    title = ver.Title;
                    break;
                }
            }

            return title;
        }
    }

    // ReSharper disable InconsistentNaming
    public class OrderedDictionary<K, T> : KeyedCollection<K, T>
    {
        private readonly Func<T, K> GetKey;

        public OrderedDictionary(Func<T, K> GetKey)
        {
            this.GetKey = GetKey;
        }

        public OrderedDictionary(Func<T, K> GetKey, IEqualityComparer<K> comparer)
            : base(comparer)
        {
            this.GetKey = GetKey;
        }

        public OrderedDictionary(Func<T, K> GetKey, IEqualityComparer<K> comparer, int dictionaryCreationThreshold)
            : base(comparer, dictionaryCreationThreshold)
        {
            this.GetKey = GetKey;
        }

        protected override K GetKeyForItem(T item)
        {
            return GetKey(item);
        }

        public bool TryGet(K key, out T value)
        {
            try
            {
                value = this[key];
                return true;
            }
            catch (KeyNotFoundException)
            {
                value = default(T);
                return false;
            }
        }
    }
    // ReSharper restore InconsistentNaming
}
