using SoftwareDebuggerExtension.SDebugger;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace SoftwareDebuggerExtension
{
    class VSTraceListener : TextWriterTraceListener
    {
        private readonly Output _output;
        private readonly StreamWriter _writer;
        public VSTraceListener(Output output)
        {
            _output = output;
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string specificFolder = Path.Combine(folder, "SoftwareDebugger");
            if (!Directory.Exists(specificFolder))
                Directory.CreateDirectory(specificFolder);
            _writer = new StreamWriter(new FileStream(Path.Combine(specificFolder, "log-debugger.txt"), FileMode.OpenOrCreate, FileAccess.Write));
        }

        public override void Write(string message)
        {
            _writer.Write(message); _writer.Flush();
        }

        public override void WriteLine(string message)
        {
            _writer.WriteLine(message); _writer.Flush();
        }
    }
}
