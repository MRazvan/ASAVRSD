using SoftwareDebuggerExtension.SDebugger;
using System.Diagnostics;

namespace SoftwareDebuggerExtension
{
    class VSOutWindowListener : TraceListener
    {
        private readonly Output _output;

        public VSOutWindowListener(Output output)
        {
            _output = output;
        }

        public override void Write(string message)
        {
            _output.DebugOut(message);
        }

        public override void WriteLine(string message)
        {
            _output.DebugOutLine(message);
        }
    }
}
