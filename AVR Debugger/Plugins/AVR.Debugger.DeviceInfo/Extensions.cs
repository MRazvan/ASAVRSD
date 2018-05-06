using System.Globalization;
using System.Xml;

namespace AVR.Debugger.DeviceInfo
{
    internal static class Extensions
    {
        public static long ToLong(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return -1;
            long val = -1;
            if (str.StartsWith("0x"))
            {
                str = str.Substring(2);
                if (long.TryParse(str, NumberStyles.HexNumber, CultureInfo.GetCultureInfo("en-EN").NumberFormat, out val))
                    return val;
                return -1;
            }
            if (long.TryParse(str, out val))
                return val;
            return -1;
        }

        public static float ToFloat(this string str)
        {
            return float.Parse(str);
        }

        public static string Value(this XmlNode node, string attr)
        {
            return node?.Attributes[attr]?.Value;
        }
    }
}
