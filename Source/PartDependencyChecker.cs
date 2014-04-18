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

        internal void Update()
        {
            // Do the requires thing. This needs to get moved to a different class.
            HashSet<string> assem = new HashSet<string>();
            foreach (var a in AssemblyLoader.loadedAssemblies)
                assem.Add(a.assembly.GetName().Name);

            foreach (UrlDir.UrlConfig urlConf in GameDatabase.Instance.root.AllConfigs)
            {
                if (urlConf.type != "PART")
                    continue;

                ConfigNode part = urlConf.config;
                if (CheckPartRequiresAssembly(assem, part))
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
    }
}
