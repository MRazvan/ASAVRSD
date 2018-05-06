using System;
using AVR.Debugger.Interfaces;
using AVR.Debugger.WatchViewer.Models;
using AVR.Debugger.WatchViewer.View;

namespace AVR.Debugger.WatchViewer
{
    [PluginInfo("Watch Viewer", "1.0")]
    public class WatchViewerPlugin : IPlugin
    {
        private static int _idx;
        private IDebuggerWrapper _debuggerWrapper;
        private IEventService _eventsService;
        private WatchDataContext _watchDataContext;

        public void Initialize(Interfaces.IServiceProvider serviceProvider)
        {
            _debuggerWrapper = serviceProvider.GetService<IDebuggerWrapper>();
            _eventsService = serviceProvider.GetService<IEventService>();
            _watchDataContext = new WatchDataContext(_debuggerWrapper);
            var host = serviceProvider.GetService<IDocumentHost>();
            host?.AddDocument("watch" + _idx,
                new WPFDocument(new WatchView {DataContext = _watchDataContext}) {Text = $"Watch"});
            _idx++;

            _watchDataContext.Locals.Add(new WatchVariable()
            {
                Name = "Test",
                Address = 1203,
                Value = 123,
                Type = "uint8_t",
                Size = 16,
                IsExpanded = false
            });

            _eventsService?.AddEventHandler(Events.Debug_Leave, (a) => _watchDataContext.InDebug = false);
            _eventsService?.AddEventHandler(Events.Debug_AfterEnter, (a) => _watchDataContext.InDebug = true);
            _eventsService?.AddEventHandler(Events.Debug_AfterEnter, _watchDataContext.Update);
        }

        public void PostInitialize(){}
    }
}