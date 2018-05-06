using System;
using AVR.Debugger.Interfaces;

namespace AVR.Debugger.CPUView
{
    [PluginInfo("CPU View", "1.0")]
    public class CpuPlugin : IPlugin
    {
        private RegistersViewModel _registersVm;

        private IDebuggerWrapper _debugger;
        private IEventService _eventService;

        public void Initialize(Interfaces.IServiceProvider serviceProvider)
        {
            _registersVm = new RegistersViewModel();

            var host = serviceProvider.GetService<IDocumentHost>();
            host?.AddDocument("CpuView", new WPFDocument(new CpuViewControl { DataContext = _registersVm }) { Text = "Cpu Info" });

            _debugger = serviceProvider.GetService<IDebuggerWrapper>();
            _eventService = serviceProvider.GetService<IEventService>();
            _eventService?.AddEventHandler(Events.Debug_Leave, DisableControl);
            _eventService?.AddEventHandler(Events.Debug_Enter, ShowCpu);
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

        public void PostInitialize() { }
    }
}
