using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace SoftwareDebuggerExtension.SDebugger
{
    public class Output
    {
        public static Guid SDSerialOutputPane = Guid.Parse("{9F79FB17-B312-4050-90D4-A90D335ABFD8}");
        public static Guid SDTraceOutputPane = Guid.Parse("{1FFE1F93-F2A8-4BC6-B83B-B88E6DD52FD8}");
        public static Guid SDDebugOutputPane = Guid.Parse("{632DD33E-25B3-4681-9D34-95971CC51FCF}");


        private readonly IServiceProvider _serviceProvider;
        private IVsOutputWindowPane _serialOutPane;
        private IVsOutputWindowPane _traceOutPane;
        private IVsOutputWindowPane _debugOutPane;

        public Output(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Initialize()
        {
            _serialOutPane = InitializePane(ref SDSerialOutputPane, "SD Serial Output", true, true);
            _traceOutPane = InitializePane(ref SDTraceOutputPane, "SD Trace Output", true, true);
            _debugOutPane = InitializePane(ref SDDebugOutputPane, "SD Debug Output", true, true);
        }

        public void Activate(Guid id)
        {
            var output = (IVsOutputWindow)_serviceProvider.GetService(typeof(SVsOutputWindow));
            IVsOutputWindowPane pane;
            output.GetPane(ref id, out pane);
            pane?.Activate();
        }

        private IVsOutputWindowPane InitializePane(ref Guid id, string title, bool visible, bool clearWithSolution)
        {
            var output = (IVsOutputWindow)_serviceProvider.GetService(typeof(SVsOutputWindow));
            IVsOutputWindowPane pane;
            output.GetPane(ref id, out pane);
            if (pane == null)
            {
                // Create a new pane.  
                output.CreatePane(
                    ref id,
                    title,
                    Convert.ToInt32(visible),
                    Convert.ToInt32(clearWithSolution));
                // Retrieve the new pane.  
                output.GetPane(ref id, out pane);
            }
            return pane;
        }

        public void SerialOut(string data)
        {
            _serialOutPane?.OutputStringThreadSafe(data);
        }

        public void TraceOut(string message)
        {
            _traceOutPane?.OutputStringThreadSafe(message);
        }

        public void DebugOut(string message)
        {
            _debugOutPane?.OutputStringThreadSafe(message);
        }

        public void DebugOutLine(string message)
        {
            _debugOutPane?.OutputStringThreadSafe(message + "\n");
        }

        public void Clear(Guid id)
        {
            var output = (IVsOutputWindow)_serviceProvider.GetService(typeof(SVsOutputWindow));
            IVsOutputWindowPane pane;
            output.GetPane(ref id, out pane);
            pane?.Clear();
        }
    }
}
