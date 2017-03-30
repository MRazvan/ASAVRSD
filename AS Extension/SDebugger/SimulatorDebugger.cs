using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Atmel.Studio.Services;
using Atmel.Studio.Services.Device;
using Atmel.VsIde.AvrStudio.Services.TargetService;
using Atmel.VsIde.AvrStudio.Services.TargetService.TCF.Core;
using Debugger.Server;
using Debugger.Server.Commands;
using Microsoft.VisualStudio.Shell.Interop;
using IServiceProvider = System.IServiceProvider;
using System.Windows.Threading;
using Atmel.VsIde.AvrStudio.Services.TargetService.TCF.Services;
using Atmel.VsIde.AvrStudio.Services.TargetService.TCF.Services.RunControl;
using EnvDTE;

namespace Microsoft.WPFWizardExample.SDebugger
{
    public enum State
    {
        None,
        Continue,
        Step,
        EnteringBreak
    }

    public class SimulatorDebugger
    {
        private DebugTarget _debugTarget;
        private uint _pc;
        private IAddressSpace _ramSpace;
        private bool _running;
        private DebugServer _server;
        private ITargetService _target;
        private DebuggerEventsProxy _events;
        private Output _output;
        private DTE _dte;

        public SimulatorDebugger(IServiceProvider serviceProvider)
        {
            _running = false;
            _dte = serviceProvider.GetService(typeof(SDTE)) as DTE;
            _server = new DebugServer();
            _events = new DebuggerEventsProxy(_server);
            _output = new Output(serviceProvider);
            _output.Initialize();
        }


        public void Start(ITransport transport)
        {
            if (_running)
                return;
            _target = ATServiceProvider.GetService<STargetService, ITargetService>();
            _debugTarget = _target.GetCurrentTarget();

            _pc = 0;
            _ramSpace = _debugTarget.Device.GetAddressSpace("data");
            
            _events.DebugEnter += EventsOnDebugEnter;
            _events.MemoryChanged += EventsOnMemoryChanged;
            _events.Start(_debugTarget);

            _server.SetTransport(transport);
            _server.UnknownData += ServerOnUnknownData;
            // Start the debug server
            _server.Start();
            _output.Activate(Output.SDSerialOutputPane);

            _running = true;
        }

        public void Step()
        {
            _dte.Debugger.StepOver(false);
            _server.Step();
        }

        public void Continue()
        {
            _dte.Debugger.Go(false);
            _server.Continue();
        }

        private void ServerOnUnknownData(byte data)
        {
            _output.SerialOut(Convert.ToChar(data) + "");
        }

        private void EventsOnMemoryChanged(string memId, long addr, long size)
        {
            if (memId != _debugTarget.GetMemType("data"))
                return;
            // The memory changed we need to update the physical device
            MemoryError[] errRanges;
            var data = _debugTarget.Memory.GetMemory(memId, (ulong) addr, 1, (int) size, 0, out errRanges);
            _server.AddCommand(new DebugCommand_Ram_Write((uint) addr, data));
        }

        private void EventsOnDebugEnter()
        {
            IStatus status;
            var ramData = WaitForCommand(new DebugCommand_Ram_Read((uint)_ramSpace.Start, (uint)_ramSpace.Size));
            if (_server.Caps.HasFlag(DebuggerCapabilities.CAPS_DBG_CTX_ADDR_BIT) &
                _server.Caps.HasFlag(DebuggerCapabilities.CAPS_SAVE_CONTEXT_BIT))
            {
                var dbgCtxData = WaitForCommand(new DebugCommand_CtxRead());
                var dbCtxAddr = (dbgCtxData[0] << 8) + dbgCtxData[1];
                // Set the registers from the saved context
                for (var i = 0; i < 32; ++i)
                    ramData[i] = ramData[dbCtxAddr + i];
                // Save the PC address
                _pc = (uint)(ramData[dbCtxAddr + 35] << 8) + ramData[dbCtxAddr + 34];
                _debugTarget.ScriptInterface.CalcValue($"$pc=0x{_pc * 2:X}");
            }

            _debugTarget.Memory.SetMemory(_debugTarget.GetMemType("data"), _ramSpace.Start, 1, ramData.Length, 0, ramData, out status);
            // Set the pc location for the simulator
            _debugTarget.NotifyTargetBreaked(
                new DebugTarget.TargetHaltedEventArgs(_pc * 2, "Extern Break", _debugTarget.ProcessesContextid, null)
            );
        }

        public void Stop()
        {
            if (!_running)
                return;

            _server.Stop();

            _target = null;
            _debugTarget = null;
            _ramSpace = null;
            _pc = 0;

            _running = false;
        }

        protected byte[] WaitForCommand(IDebugCommand command)
        {
            var task = _server.AddCommand(command);
            task.Wait();
            return task.Result;
        }
    }
}