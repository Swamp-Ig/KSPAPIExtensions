using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

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
                              where ass.assembly.GetType(targetCls.FullName, false) != null
                              && ass.assembly.GetName().Name == assemName
                              orderby ass.assembly.GetName().Version descending, ass.path ascending
                              select ass).ToArray();
            var winner = candidates.First();

            if (targetCls.Assembly != winner.assembly)
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
    }
}
