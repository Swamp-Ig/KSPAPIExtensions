using System;
using System.Linq;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace KSPAPIExtensions
{
    /// <summary>
    /// Utility methods for parsing things
    /// </summary>
    public static class ParseUtils
    {
        /// <summary>
        /// Tries to parse a Vector3 from the given string. Returns false if anything goes wrong.
        /// </summary>
        /// <param name="text">String to parse</param>
        /// <param name="result">The result, or default value if fails</param>
        /// <returns>True if successful</returns>
        public static bool TryParseVector3(string text, out Vector3 result)
        {
            try
            {
                result = ParseVector3(text);
                return true;
            }
            catch (ArgumentNullException) { }
            catch (FormatException) { }
            result = default(Vector3);
            return false;
        }

        /// <summary>
        /// Parses a Vector3 from the given string.
        /// </summary>
        /// <param name="text">String to parse</param>
        /// <returns>The parsed vector</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="text"/> is null</exception>
        /// <exception cref="FormatException">If <paramref name="text"/> cannot be parsed, or one of the component values would overflow a float</exception>
        public static Vector3 ParseVector3(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            string[] splits = text.Split(',').Select(s => s.Trim()).ToArray();
            if (splits.Length != 3)
                throw new FormatException("Unable to parse string as Vector3");

            try
            {
                return new Vector3(float.Parse(splits[0]), float.Parse(splits[1]), float.Parse(splits[2]));
            }
            catch (OverflowException ex)
            {
                throw new FormatException("Unable to parse string as Vector3", ex);
            }
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Tries to parse a Vector3d from the given string. 
        /// </summary>
        /// <param name="text">String to parse</param>
        /// <param name="result">The result, or default value if fails</param>
        /// <returns>True if successful</returns>
        public static bool TryParseVector3d(string text, out Vector3d result)
        {
            try
            {
                result = ParseVector3d(text);
                return true;
            }
            catch (ArgumentNullException) { }
            catch (FormatException) { }
            result = default(Vector3d);
            return false;
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Parses a Vector3d from the given string. 
        /// </summary>
        /// <param name="text">String to parse</param>
        /// <returns>The parsed vector</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="text"/> is null</exception>
        /// <exception cref="FormatException">If <paramref name="text"/> cannot be parsed, or one of the component values would overflow a double</exception>
        public static Vector3d ParseVector3d(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            string[] splits = text.Split(',').Select(s => s.Trim()).ToArray();
            if (splits.Length != 3)
                throw new FormatException("Unable to parse string as Vector3");

            try
            {
                return new Vector3d(double.Parse(splits[0]), double.Parse(splits[1]), double.Parse(splits[2]));
            }
            catch (OverflowException ex)
            {
                throw new FormatException("Unable to parse string as Vector3", ex);
            }
        }

        /// <summary>
        /// Tries to parse a Color from the given string. 
        /// </summary>
        /// <param name="text">String to parse</param>
        /// <param name="result">The result, or default value if fails</param>
        /// <returns>True if successful</returns>
        public static bool TryParseColor(string text, out Color result)
        {
            try
            {
                result = ParseColor(text);
                return true;
            }
            catch (ArgumentNullException) { }
            catch (FormatException) { }
            result = default(Color);
            return false;
        }

        /// <summary>
        /// Parses a Color from the given string. 
        /// </summary>
        /// <param name="text">String to parse</param>
        /// <returns>The parsed Color</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="text"/> is null</exception>
        /// <exception cref="FormatException">If <paramref name="text"/> cannot be parsed, or one of the component values would overflow a float</exception>
        public static Color ParseColor(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            string[] splits = text.Split(',').Select(s => s.Trim()).ToArray();
            try
            {
                switch (splits.Length)
                {
                    case 3:
                        return new Color(float.Parse(splits[0]), float.Parse(splits[1]), float.Parse(splits[2]));
                    case 4:
                        return new Color(float.Parse(splits[0]), float.Parse(splits[1]), float.Parse(splits[2]), float.Parse(splits[3]));
                    default:
                        throw new FormatException("Unable to parse string as Color");
                }
            }
            catch (OverflowException ex)
            {
                throw new FormatException("Unable to parse string as Color", ex);
            }
        }

        /// <summary>
        /// Parses the given string as an array where each element is separated by a comma.
        /// </summary>
        /// <param name="text">String to parse</param>
        /// <returns>The array, or null if <paramref name="text"/> is null</returns>
        public static string[] ParseArray(string text)
        {
            if (text == null)
                return null;
            return text.Split(',').Select(s => s.Trim()).ToArray();
        }
    }
}
