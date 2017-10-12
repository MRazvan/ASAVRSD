using System;
using AVR.Debugger.Interfaces;

namespace CPUView
{
    public class CpuPlugin : IPlugin
    {
        public string Name => "CPU View";
        private RegistersViewModel _registersVm;

        private IDebuggerWrapper _debugger;

        public void Initialize(IServiceProvider serviceProvider)
        {
            _registersVm = new RegistersViewModel();

            var host = serviceProvider.GetService(typeof(IDocumentHost)) as IDocumentHost;
            host?.AddDocument("CpuView", new WPFDocument(new CpuViewControl { DataContext = _registersVm }) { Text = "Cpu Info" });

            _debugger = serviceProvider.GetService(typeof(IDebuggerWrapper)) as IDebuggerWrapper;
            _debugger?.AddEventHandler(Events.DebugLeave, DisableControl);
            _debugger?.AddEventHandler(Events.DebugEnter, ShowCpu);
        }

        private void DisableControl()
        {
            _registersVm.InDebug = false;
        }

        private void ShowCpu()
        {
            _registersVm.UpdateCpuState(_debugger.CpuState);
            _registersVm.InDebug = true;
        }
    }
}
