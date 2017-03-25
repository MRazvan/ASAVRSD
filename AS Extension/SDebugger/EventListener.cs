using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Atmel.Studio.Services;
using Atmel.VsIde.AvrStudio.Services.TargetService;
using Atmel.VsIde.AvrStudio.Services.TargetService.TCF.Protocol;
using Atmel.VsIde.AvrStudio.Services.TargetService.TCF.Services;
using Debugger.Server;
using Debugger.Server.Commands;

namespace Microsoft.WPFWizardExample.SDebugger
{
    public class EventListener : IEventListener
    {
        private readonly DebugTarget _target;
        private readonly IDebugServer _server;
        private readonly string _dataMemory;
        private bool _updateSuspended;

        public Action ContextSuspended;
        public Action ContextResumed;

        public EventListener(DebugTarget target, IDebugServer server, string dataMemory)
        {
            _target = target;
            _server = server;
            _dataMemory = dataMemory;
        }

        public void Event(string name, byte[] data)
        {
            if (!_server.InDebug)
                return;
            if (name == "contextSuspended")
            {
                ContextSuspended?.Invoke();
                return;
            }
            if (name == "contextResumed")
            {
                ContextResumed?.Invoke();
                return;
            }
            object[] sequence = JSON.ParseSequence(data);
            if (name == "memoryChanged")
            {
                HandleMemoryChanged(sequence);
            }
        }

        private void HandleMemoryChanged(object[] sequence)
        {
            if (_updateSuspended)
                return;

            if (sequence.Length < 2)
                return;

            var memId = sequence[0] as string;
            if (string.IsNullOrWhiteSpace(memId))
                return;
            if (memId != _dataMemory)
                return;

            // Get the address and the size
            var memChangedData = sequence[1] as List<object>;
            if (memChangedData == null || memChangedData.Count != 1)
                return;
            var properties = memChangedData[0] as Dictionary<string, object>;
            if (properties == null || properties.Count != 2)
                return;
            if (!properties.ContainsKey("addr") || !properties.ContainsKey("size"))
                return;

            var memAddr = (long) properties["addr"];
            var memSize = (long) properties["size"];

            TargetService.MainThreadDispatcher.BeginInvoke(
                new Action<DebugTarget, IDebugServer, long, long>((target, server, addr, size) => {
                    MemoryError[] errRanges;
                    IStatus status;
                    var data = target.Memory.GetMemory(target.GetMemType("data"), (ulong) addr, 1, (int) size, 0, out errRanges, out status);
                    if (errRanges.Any())
                        return;

                    server.AddCommand(new DebugCommand_Ram_Write((uint) addr, data));

                }), DispatcherPriority.Background, _target, _server, memAddr, memSize);
        }

        public void ResumeUpdate()
        {
            _updateSuspended = false;
        }

        public void SuspendUpdate()
        {
            _updateSuspended = true;
        }
    }
}