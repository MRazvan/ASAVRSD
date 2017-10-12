using System;
using AVR.Debugger.Interfaces;
using WatchViewer.Models;
using WatchViewer.View;

namespace WatchViewer
{
    public class WatchViewerPlugin : IPlugin
    {
        private static int _idx;
        private IDebuggerWrapper _debuggerWrapper;
        private WatchDataContext _watchDataContext;
        public string Name => "Watch Viewer";

        public void Initialize(IServiceProvider serviceProvider)
        {
            _debuggerWrapper = serviceProvider.GetService(typeof(IDebuggerWrapper)) as IDebuggerWrapper;
            _watchDataContext = new WatchDataContext(_debuggerWrapper);
            var host = serviceProvider.GetService(typeof(IDocumentHost)) as IDocumentHost;
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

            _debuggerWrapper?.AddEventHandler(Events.AfterDebugEnter, _watchDataContext.Update);
            _debuggerWrapper?.AddEventHandler(Events.DebugLeave, () => _watchDataContext.InDebug = false);
            _debuggerWrapper?.AddEventHandler(Events.AfterDebugEnter, () => _watchDataContext.InDebug = true);
        }
    }
}