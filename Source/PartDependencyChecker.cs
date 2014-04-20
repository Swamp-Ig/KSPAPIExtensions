using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPAPIExtensions
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    internal class PartDependencyChecker : MonoBehaviour
    {

        // Use the Update method to be sure this runs *after* module manager. (MM used OnGUI)
        internal void Update()
        {
            // If we're not ready to run, return and wait for the next Update
            if (!GameDatabase.Instance.IsReady() && !GameSceneFilter.AnyInitializing.IsLoaded())
                return;

            try
            {
                // Run the type election
                if (!SystemUtils.RunTypeElection(typeof(PartDependencyChecker), "KSPAPIExtensions"))
                    return;

                // Get a set of assemblies
                HashSet<string> assem = new HashSet<string>();
                foreach (var a in AssemblyLoader.loadedAssemblies)
                    assem.Add(a.assembly.GetName().Name);

                // Filter the parts list
                foreach (UrlDir.UrlConfig urlConf in GameDatabase.Instance.root.GetConfigs("PART").ToArray())
                {
                    if (CheckPartRequiresAssembly(assem, urlConf.config))
                    {
                        Debug.Log("[PartDependencyChecker] removing part " + urlConf.name + " due to dependency requirements not met");
                        urlConf.parent.configs.Remove(urlConf);
                    }
                    else
                    {
                        urlConf.config.RemoveValues("RequiresAssembly");
                    }
                }
            }
            finally
            {
                // Destroy ourself because there's no reason to still hang around
                UnityEngine.Object.Destroy(gameObject);
                enabled = false;
            }
        }

        private static bool CheckPartRequiresAssembly(HashSet<string> assem, ConfigNode part)
        {
            foreach (string keyValue in part.GetValues("RequiresAssembly"))
            {
                foreach (string split in keyValue.Split(','))
                {
                    string value = split.Trim();
                    if (!string.IsNullOrEmpty(value) && (value[0] == '!' ? assem.Contains(value.Substring(1).Trim()) : !assem.Contains(value)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
