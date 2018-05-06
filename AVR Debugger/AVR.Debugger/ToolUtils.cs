using System.IO;
using System.Reflection;

namespace AVR.Debugger
{
    public static class ToolUtils
    {
        public static string ToolsPath
        {
            get { return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "gcc"); }
        }

        public static string ObjDump
        {
            get { return Path.Combine(ToolsPath, "avr-objdump.exe"); }
        }
    }
}