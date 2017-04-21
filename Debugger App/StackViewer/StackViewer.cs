using System;
using System.Windows.Forms;
using AVR.Debugger.Interfaces;

namespace StackViewer
{
    public class StackViewer : IPlugin
    {
        public string Name => "Stack viewer";

        private IDocumentHost _host;
        private IDebuggerWrapper _debuggerWrapper;


        public void Initialize(IServiceProvider serviceProvider)
        {
            _host = serviceProvider.GetService(typeof(IDocumentHost)) as IDocumentHost;
            _debuggerWrapper = serviceProvider.GetService(typeof(IDebuggerWrapper)) as IDebuggerWrapper;
        }
    }
}
