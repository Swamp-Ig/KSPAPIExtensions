using System.Linq;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace KSPAPIExtensions
{
    public static class ConfigNodeUtils
    {
        #region TryGetValue

        /// <summary>
        /// Get value from the node.
        /// </summary>
        /// <param name="node">Node to get value from</param>
        /// <param name="name">Name of the value to find</param>
        /// <param name="value">The result, or default value if fails</param>
        /// <returns>True if successful. False if the value is not present or can't be parsed.</returns>
        public static bool TryGetValue(this ConfigNode node, string name, out string value)
        {
            value = node.GetValue(name);
            return value != null;
        }

        /// <summary>
        /// Get the value and parse as a string array.
        /// </summary>
        /// <param name="node">Node to get value from</param>
        /// <param name="name">Name of the value to find</param>
        /// <param name="value">The result, or null if fails</param>
        /// <returns>True if successful. False if the value is not present or can't be parsed.</returns>
        public static bool TryGetValue(this ConfigNode node, string name, out string[] value)
        {
            value = ParseUtils.ParseArray(node.GetValue(name));
            return value != null;
        }

        /// <summary>
        /// Get the value and parse as a boolean.
        /// </summary>
        /// <param name="node">Node to get value from</param>
        /// <param name="name">Name of the value to find</param>
        /// <param name="value">The result, or false if fails</param>
        /// <returns>True if successful. False if the value is not present or can't be parsed.</returns>
        public static bool TryGetValue(this ConfigNode node, string name, out bool value)
        {
            string val = node.GetValue(name);
            if (string.IsNullOrEmpty(val))
            {
                value = false;
                return false;
            }
            return bool.TryParse(val, out value);
        }

        /// <summary>
        /// Get the value and parse as an int.
        /// </summary>
        /// <param name="node">Node to get value from</param>
        /// <param name="name">Name of the value to find</param>
        /// <param name="value">The result, or 0 if fails</param>
        /// <returns>True if successful. False if the value is not present or can't be parsed.</returns>
        public static bool TryGetValue(this ConfigNode node, string name, out int value)
        {
            string val = node.GetValue(name);
            if (string.IsNullOrEmpty(val))
            {
                value = 0;
                return false;
            }
            return int.TryParse(val, out value);
        }

        /// <summary>
        /// Get the value and parse as a float.
        /// </summary>
        /// <param name="node">Node to get value from</param>
        /// <param name="name">Name of the value to find</param>
        /// <param name="value">The result, or 0 if fails</param>
        /// <returns>True if successful. False if the value is not present or can't be parsed.</returns>
        public static bool TryGetValue(this ConfigNode node, string name, out float value)
        {
            string val = node.GetValue(name);
            if (string.IsNullOrEmpty(val))
            {
                value = 0;
                return false;
            }
            return float.TryParse(val, out value);
        }

        /// <summary>
        /// Get the value and parse as a double.
        /// </summary>
        /// <param name="node">Node to get value from</param>
        /// <param name="name">Name of the value to find</param>
        /// <param name="value">The result, or 0 if fails</param>
        /// <returns>True if successful. False if the value is not present or can't be parsed.</returns>
        public static bool TryGetValue(this ConfigNode node, string name, out double value)
        {
            string val = node.GetValue(name);
            if (string.IsNullOrEmpty(val))
            {
                value = 0;
                return false;
            }
            return double.TryParse(val, out value);
        }

        /// <summary>
        /// Get the value and parse as a Vector3.
        /// </summary>
        /// <param name="node">Node to get value from</param>
        /// <param name="name">Name of the value to find</param>
        /// <param name="value">The result, or default value if fails</param>
        /// <returns>True if successful. False if the value is not present or can't be parsed.</returns>
        public static bool TryGetValue(this ConfigNode node, string name, out Vector3 value)
        {
            string val = node.GetValue(name);
            if (string.IsNullOrEmpty(val))
            {
                value = default(Vector3);
                return false;
            }
            return ParseUtils.TryParseVector3(val, out value);
        }

        /// <summary>
        /// Get the value and parse as a Vector3d.
        /// </summary>
        /// <param name="node">Node to get value from</param>
        /// <param name="name">Name of the value to find</param>
        /// <param name="value">The result, or default value if fails</param>
        /// <returns>True if successful. False if the value is not present or can't be parsed.</returns>
        public static bool TryGetValue(this ConfigNode node, string name, out Vector3d value)
        {
            string val = node.GetValue(name);
            if (string.IsNullOrEmpty(val))
            {
                value = default(Vector3d);
                return false;
            }
            return ParseUtils.TryParseVector3d(val, out value);
        }

        /// <summary>
        /// Get the value and parse as a Color.
        /// </summary>
        /// <param name="node">Node to get value from</param>
        /// <param name="name">Name of the value to find</param>
        /// <param name="value">The result, or default value if fails</param>
        /// <returns>True if successful. False if the value is not present or can't be parsed.</returns>
        public static bool TryGetValue(this ConfigNode node, string name, out Color value)
        {
            string val = node.GetValue(name);
            if (string.IsNullOrEmpty(val))
            {
                value = default(Color);
                return false;
            }
            return ParseUtils.TryParseColor(val, out value);
        }
        #endregion

        #region TryGetNode
        /// <summary>
        /// Get the config node, returns true if successful.
        /// </summary>
        /// <param name="node">Node to get value from</param>
        /// <param name="name">Name of the node to find</param>
        /// <param name="result">The result, or null if fails</param>
        /// <returns>True if successful. False if the value is not present.</returns>
        public static bool TryGetNode(this ConfigNode node, string name, out ConfigNode result)
        {
            result = node.GetNode(name);
            return result != null;
        }

        /// <summary>
        /// Sees if the node has any ConfigNode of the given name, and stores all the found occurences in the ref value. Returns false if there was none.
        /// </summary>
        /// <param name="node">Node to get value from</param>
        /// <param name="name">Name of the nodes to find</param>
        /// <param name="results">Array to store the results in</param>
        /// <returns>True if successful. False if the value is not present.</returns>
        public static bool TryGetNodes(this ConfigNode node, string name, out ConfigNode[] results)
        {
            results = node.GetNodes(name);
            return results.Length != 0;
        }

        /// <summary>
        /// Sees if the node has any value of the given name, and stores all occurences found in the ref value. Returns false if there was none.
        /// </summary>
        /// <param name="node">Node to get value from</param>
        /// <param name="name">Name of the values to find</param>
        /// <param name="results">Array to store the results in</param>
        /// <returns>True if successful. False if the value is not present.</returns>
        public static bool TryGetValues(this ConfigNode node, string name, out string[] results)
        {
            results = node.GetValues(name);
            return results.Length != 0;
        }
        #endregion

        #region HasValues / HasNodes
        /// <summary>
        /// Checks to see if the ConfigNode has all specified values
        /// </summary>
        /// <param name="node">Node to get values from</param>
        /// <param name="values">Values to find</param>
        public static bool HasAllValues(this ConfigNode node, params string[] values)
        {
            return values.All(node.HasValue);
        }

        /// <summary>
        /// Checks to see if the given ConfigNode has all specified nodes.
        /// </summary>
        /// <param name="node">Node to get nodes from</param>
        /// <param name="nodes">Nodes to find</param>
        public static bool HasAllNodes(this ConfigNode node, params string[] nodes)
        {
            return nodes.All(node.HasNode);
        }
        #endregion
    }
}
