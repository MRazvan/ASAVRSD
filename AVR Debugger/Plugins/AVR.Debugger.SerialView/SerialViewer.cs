using System;
using AVR.Debugger.Interfaces;
using AVR.Debugger.SerialView.View;

namespace AVR.Debugger.SerialViewer
{
    [PluginInfo("Serial Viewer", "1.0")]
    public class SerialViewer : IPlugin
    {
        private IDocumentHost _host;
        private IDebuggerWrapper _debuggerWrapper;
        private SerialViewModel _serialViewModel;
        private IEventService _eventsService;

        public void Initialize(Interfaces.IServiceProvider serviceProvider)
        {
            _host = serviceProvider.GetService<IDocumentHost>();
            _debuggerWrapper = serviceProvider.GetService<IDebuggerWrapper>();
            _eventsService = serviceProvider.GetService<IEventService>();
            _serialViewModel = new SerialViewModel();

            _host?.AddDocument("serialViewer", new WPFDocument(new SerialViewControl { DataContext = _serialViewModel }) { Text = "Serial" });
            _eventsService?.AddEventHandler(Events.Debug_UnknownData, HandleData);
        }

        private void HandleData(object obj)
        {
            _serialViewModel.ShowCharacter(Convert.ToChar(obj));
        }

        public void PostInitialize() { }
    }
}
