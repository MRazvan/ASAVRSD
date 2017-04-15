using System.Diagnostics;

namespace AVR.Debugger
{
    class AvrDisassembler
    {
        public string Disassemble(string file)
        {
            var process = new Process();
            var si = new ProcessStartInfo(ToolUtils.ObjDump);
            si.Arguments = $"-d \"{file}\"";
            si.CreateNoWindow = true;
            si.WindowStyle = ProcessWindowStyle.Hidden;
            si.UseShellExecute = false;
            si.RedirectStandardError = true;
            si.RedirectStandardOutput = true;

            process.StartInfo = si;
            process.Start();
            var data = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return data;
        }
    }
}
