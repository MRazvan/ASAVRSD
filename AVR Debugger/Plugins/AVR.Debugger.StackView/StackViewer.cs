using System;
using System.Windows.Forms;
using AVR.Debugger.Interfaces;

namespace AVR.Debugger.StackViewer
{
    [PluginInfo("Stack Viewer", "1.0")]
    public class StackViewer : IPlugin
    {
        private IDocumentHost _host;
        private IDebuggerWrapper _debuggerWrapper;


        public void Initialize(Interfaces.IServiceProvider serviceProvider)
        {
            _host = serviceProvider.GetService<IDocumentHost>();
            _debuggerWrapper = serviceProvider.GetService<IDebuggerWrapper>();
        }

        public void PostInitialize(){}
    }
}
