using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace KSPAPIEL
{
    public static class MathUtils
    {
        /// <summary>
        /// Format a numeric value using SI prefexes. 
        /// 
        /// eg: 13401 -> 13.4 k
        /// 
        /// </summary>
        /// <param name="value">value to format</param>
        /// <param name="unit">unit string</param>
        /// <param name="exponent">Exponennt to the existing value. eg: if value was km rather than m this would be 3.</param>
        /// <param name="sigFigs">number of signifigant figures to display</param>
        /// <returns></returns>
        public static string ToStringSI(this double value, int sigFigs = 3, int exponent = 0, string unit = null)
        {
            SIPrefix prefix = value.GetSIPrefix(exponent);
            return prefix.FormatSI(value, sigFigs, exponent, unit);
        }

        /// <summary>
        /// Format a numeric value using SI prefexes. 
        /// 
        /// eg: 13401 -> 13.4 k
        /// 
        /// </summary>
        /// <param name="value">value to format</param>
        /// <param name="unit">unit string</param>
        /// <param name="exponent">Exponennt to the existing value. eg: if value was km rather than m this would be 3.</param>
        /// <param name="sigFigs">number of signifigant figures to display</param>
        /// <returns></returns>
        public static string ToStringSI(this float value, int sigFigs = 3, int exponent = 0, string unit = null)
        {
            SIPrefix prefix = value.GetSIPrefix(exponent);
            return prefix.FormatSI(value, sigFigs, exponent, unit);
        }

        internal static string FormatSI(this SIPrefix pfx, double value, int sigFigs = 3, int exponent = 0, string unit = null)
        {
            return string.Format("{0}{1}{2}", pfx.GetFormatter(value, sigFigs, exponent)(value), pfx.PrefixString(), unit);
        }

        /// <summary>
        /// This extension of the standard string format method has available an extra standard format.
        ///
        /// Examples:
        /// <list type="bullet">
        /// <item>12.ToStringExt("S") -> "12"</item>
        /// <item>12.ToStringExt("S3") -> "12.0"</item>
        /// <item>120.ToStringExt("S3") -> "120"</item>
        /// <item>1254.ToStringExt("S3") -> "1250"  (4 digit numbers do not use k as a special case)</item>
        /// <item>12540.ToStringExt("S3") -> "1.25k"  (using SI prefixes)</item>
        /// <item>12540.ToStringExt("S4") -> "1.254k"  (more significant figures)</item>
        /// <item>(1.254).ToStringExt("S4+3") -> "1.254k"  (+3 means the 'natural prefix' is k)</item>
        /// <item>(1.254).ToStringExt("S4-3") -> "1.254m"  (-3 means the 'natural prefix' is m)</item>
        /// </list>
        /// </summary>
        public static string ToStringExt(this double value, string format)
        {
			if (format.Length > 0 && (format[0] == 'S' || format[0] == 's'))
            {
                if (format.Length == 1)
                    return ToStringSI(value, 0);
                int pmi = format.IndexOf('+');
                int sigFigs;
                if (pmi < 0)
                {
                    pmi = format.IndexOf('-');
                    if (pmi < 0)
                    {
                        sigFigs = int.Parse(format.Substring(1));
                        return ToStringSI(value, sigFigs);
                    }
                }
                sigFigs = int.Parse(format.Substring(1, pmi - 1));
                int exponent = int.Parse(format.Substring(pmi));
                return ToStringSI(value, sigFigs, exponent);
            }
            return value.ToString(format);
        }

        /// <summary>
        /// ToStringExt for floats. See doc for doubles.
        /// </summary>
        public static string ToStringExt(this float value, string format)
        {
            return ToStringExt((double)value, format);
        }

        /// <summary>
        /// Find the SI prefix for a number
        /// </summary>
        /// <param name="value">The value to find the prefix for</param>
        /// <param name="exponent">The natural exponent, if your value was km rather than m, use 3</param>
        public static SIPrefix GetSIPrefix(this double value, int exponent = 0)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (value == 0)
                return SIPrefix.None;

            int exp = (int)Math.Floor(Math.Log10(Math.Abs(value))) + exponent;

            if (exp <= 3 && exp >= -1)
                return SIPrefix.None;
            if (exp < 0)
                return (SIPrefix)((exp-2) / 3 * 3);
            return (SIPrefix)(exp / 3 * 3);
        }

        /// <summary>
        /// Find the SI prefix for a number
        /// </summary>
        public static SIPrefix GetSIPrefix(this float value, int exponent = 0)
        {
            return GetSIPrefix((double)value, exponent);
        }

        /// <summary>
        /// The prefix string for a particular exponent. Note Micro will use the string "mic" rather than "µ", so is longer.
        /// </summary>
        public static string PrefixString(this SIPrefix pfx)
        {
            switch (pfx)
            {
                case SIPrefix.None: return "";
                case SIPrefix.Kilo: return "k";
                case SIPrefix.Mega: return "M";
                case SIPrefix.Giga: return "G";
                case SIPrefix.Tera: return "T";
                case SIPrefix.Peta: return "P";
                case SIPrefix.Exa: return "E";
                case SIPrefix.Zotta: return "Z";
                case SIPrefix.Yotta: return "Y";
                case SIPrefix.Milli: return "m";
                case SIPrefix.Micro: return "u";
                case SIPrefix.Nano: return "n";
                case SIPrefix.Pico: return "p";
                case SIPrefix.Femto: return "f";
                case SIPrefix.Atto: return "a";
                case SIPrefix.Zepto: return "z";
                case SIPrefix.Yocto: return "y";
                default: throw new ArgumentException("Illegal prefix", "pfx");
            }
        }

        /// <summary>
        /// Round a value with respect the the given SI prefix.
        /// 
        /// eg:
        /// SIPrefix.Mega.Round(34.456e6f, digits:1) -> 34.6e6f
        /// 
        /// </summary>
        /// <param name="pfx">The SI prefix</param>
        /// <param name="value">Value to round</param>
        /// <param name="digits">Number of decimal places. Negatives will work (eg: -1 rounds to nearest 10)</param>
        /// <param name="exponent">Natural exponent of value, eg: use 3 if value is km rather than m</param>
        /// <returns></returns>
        public static float Round(this SIPrefix pfx, float value, int digits = 3, int exponent = 0)
        {
            float div = Mathf.Pow(10, (int)pfx - digits + exponent);
            return Mathf.Round(value / div) * div;
        }

        /// <summary>
        /// Round a value with respect the the given SI prefix.
        /// 
        /// eg:
        /// SIPrefix.Mega.Round(34.456e6, digits:1) -> 34.6e6
        /// 
        /// </summary>
        /// <param name="pfx">The SI prefix</param>
        /// <param name="value">Value to round</param>
        /// <param name="digits">Number of decimal places. Negatives will work (eg: -1 rounds to nearest 10)</param>
        /// <param name="exponent">Natural exponent of value, eg: use 3 if value is km rather than m</param>
        /// <returns></returns>
        public static double Round(this SIPrefix pfx, double value, int digits = 3, int exponent = 0)
        {
            double div = Math.Pow(10, (int)pfx - digits + exponent);
            return Math.Round(value / div) * div;
        }

        /// <summary>
        /// Get a formatter function. This is useful when you have a range of values you'd like formatted the same way.
        /// </summary>
        public static Func<double, string> GetFormatter(this SIPrefix pfx, double value, int sigFigs = 3, int exponent = 0)
        {
            int exp = (int)(Math.Floor(Math.Log10(Math.Abs(value)))) - (int)pfx + exponent;
            double div = Math.Pow(10, (int)pfx - exponent);

            if (exp < 0)
                return v => (v/div).ToString("F" + (sigFigs-1));
            if (exp >= sigFigs)
            {
                double mult = Math.Pow(10, exp - sigFigs + 1);
                return v => (Math.Round(v / div / mult) * mult).ToString("F0");
            }
            return v => (v/div).ToString("F" + (sigFigs - exp - 1));
        }
    }

    /// <summary>
    /// SI Prefix. If cast as an int, the base 10 log for the prefix will be produced.
    /// </summary>
    public enum SIPrefix
    {
        None = 0,
        Kilo = 3,
        Mega = 6,
        Giga = 9,
        Tera = 12,
        Peta = 15,
        Exa = 18,
        Zotta = 21,
        Yotta = 24,
        Milli = -3,
        Micro = -6,
        Nano = -9,
        Pico = -12,
        Femto = -15,
        Atto = -18,
        Zepto = -21,
        Yocto = -24
    }
}