using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AVR.Debugger
{
    class AvrAddrToLine
    {
        public LineInfo GetLineInfo(uint addr, string file)
        {
            var process = new Process();
            var si = new ProcessStartInfo(ToolUtils.Addr2Line);
            si.Arguments = $"-e \"{file}\" 0x{addr:x}";
            si.CreateNoWindow = true;
            si.WindowStyle = ProcessWindowStyle.Hidden;
            si.UseShellExecute = false;
            si.RedirectStandardError = true;
            si.RedirectStandardOutput = true;

            process.StartInfo = si;
            process.Start();
            var data = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (data.LastIndexOf(":") == -1 || data.Contains("?"))
                return null;
            var sourceFile = data.Substring(0, data.LastIndexOf(":"));
            var lineStr = data.Substring(data.LastIndexOf(":") + 1).Split().First();
            int line;
            if (!int.TryParse(lineStr, out line))
                return null;

            sourceFile = Path.GetFullPath(sourceFile);

            return new LineInfo { File = sourceFile, Line = line-1 };
        }
    }
}
