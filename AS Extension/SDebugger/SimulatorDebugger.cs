using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Threading;
using Atmel.Studio.Services;
using Atmel.Studio.Services.Device;
using Atmel.VsIde.AvrStudio.Services.TargetService;
using Atmel.VsIde.AvrStudio.Services.TargetService.TCF.Services;
using Debugger.Server;
using Debugger.Server.Commands;
using Debugger.Server.Transports;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace SoftwareDebuggerExtension.SDebugger
{
    public class SimulatorDebugger
    {
        public delegate void DebugStateChangedDelegate();

        private static readonly Dictionary<string, object> sEmptyContextData = new Dictionary<string, object>();
        private readonly DTE _dte;
        private readonly DebuggerEventsProxy _events;
        private readonly Output _output;
        private readonly DebugServer _server;

        private DebugTarget _debugTarget;
        private uint _pc;
        private IAddressSpace _ramSpace;
        private bool _running;
        private State _state;
        private ITargetService _target;

        public IDebugServer DebugServer => _server;
        public event DebugStateChangedDelegate DebugStateChanged;

        public bool CanRun => _events.InDebug && _server.InDebug && _state == State.InDebug;

        public SimulatorDebugger(IServiceProvider serviceProvider, Output output)
        {
            _output = output;
            _running = false;
            _dte = serviceProvider.GetService(typeof(SDTE)) as DTE;
            _server = new DebugServer();
            _events = new DebuggerEventsProxy(_server, _output);
        }

        public void Start(ITransport transport)
        {
            if (_running)
                return;
            _state = State.None;
            _target = ATServiceProvider.GetService<STargetService, ITargetService>();
            _debugTarget = _target.GetCurrentTarget();

            _pc = 0;
            _ramSpace = _debugTarget.Device.GetAddressSpace("data");

            _events.DebugLeave += EventsOnDebugLeave;
            _events.DebugEnter += EventsOnDebugEnter;
            _events.MemoryChanged += EventsOnMemoryChanged;
            _events.Start(_debugTarget);

            _server.SetTransport(transport);
            _server.UnknownData += ServerOnUnknownData;
            _server.DebuggerAttached += ServerDebuggerAttached;
            // Start the debug server
            _server.Start();
            _output.Activate(Output.SDSerialOutputPane);

            DebugStateChanged?.Invoke();
            _running = true;
        }

        public void Stop()
        {
            if (!_running)
                return;

            _pc = 0;

            _events.DebugLeave -= EventsOnDebugLeave;
            _events.DebugEnter -= EventsOnDebugEnter;
            _events.MemoryChanged -= EventsOnMemoryChanged;
            _events.Stop(_debugTarget);

            _server.UnknownData -= ServerOnUnknownData;
            _server.DebuggerAttached -= ServerDebuggerAttached;
            // Start the debug server
            _server.Stop();

            _debugTarget = null;
            _target = null;
            _state = State.None;
            DebugStateChanged?.Invoke();
            _running = false;
        }

        private void EventsOnDebugLeave()
        {
            if (_state == State.Continue)
                _server.Continue();
            else if (_state == State.Step)
                _server.Step();
        }

        private void ServerDebuggerAttached()
        {
            DebugWrite(MethodBase.GetCurrentMethod().Name + " " + _dte.Debugger.CurrentMode +
                       $" {_debugTarget.TargetState}");
            if (_state != State.Step)
            {
                IStatus status;
                _debugTarget.Suspend(out status);
            }
        }

        public void Step()
        {
            if (!CanRun)
                return;
            DebugWrite(MethodBase.GetCurrentMethod().Name + " " + _dte.Debugger.CurrentMode +
                       $" {_debugTarget.TargetState}");
            _state = State.Step;
            IStatus status;
            _debugTarget.SDM_Step(1, out status);
            DebugStateChanged?.Invoke();
        }

        public void Continue()
        {
            if (!CanRun)
                return;
            DebugWrite(MethodBase.GetCurrentMethod().Name + " " + _dte.Debugger.CurrentMode +
                       $" {_debugTarget.TargetState}");
            _state = State.Continue;
            IStatus status;
            _debugTarget.Resume(0x0, 0x0, out status);
            DebugStateChanged?.Invoke();
        }

        public void ResetTarget()
        {
            _server.ResetTarget();
        }

        private void ServerOnUnknownData(byte data)
        {
            _output.SerialOut(Convert.ToChar(data) + "");
        }

        private void EventsOnMemoryChanged(string memId, long addr, long size)
        {
            DebugWrite(MethodBase.GetCurrentMethod().Name + " " + _dte.Debugger.CurrentMode +
                       $" {_debugTarget.TargetState}");

            MemoryError[] errRanges;
            if (memId== _debugTarget.GetMemType("data") && _server.Caps.HasFlag(DebuggerCapabilities.CAPS_RAM_W_BIT))
            {
                // The memory changed we need to update the physical device
                var data = _debugTarget.Memory.GetMemory(memId, (ulong) addr, 1, (int) size, 0, out errRanges);
                _server.AddCommand(new DebugCommand_Ram_Write((uint) addr, data));
            }
            if (memId == _debugTarget.GetMemType("eeprom") && _server.Caps.HasFlag(DebuggerCapabilities.CAPS_EEPROM_W_BIT))
            {
                var data = _debugTarget.Memory.GetMemory(memId, (ulong)addr, 1, (int)size, 0, out errRanges);
                _server.AddCommand(new DebugCommand_EEPROM_Write((uint)addr, data));
            }

        }

        private void EventsOnDebugEnter()
        {
            DebugWrite(MethodBase.GetCurrentMethod().Name + " " + _dte.Debugger.CurrentMode +
                       $" {_debugTarget.targetIsStopped}");

            IStatus status;
            var ramData = WaitForCommand(new DebugCommand_Ram_Read((uint) _ramSpace.Start, (uint) _ramSpace.Size));
            if (_server.Caps.HasFlag(DebuggerCapabilities.CAPS_DBG_CTX_ADDR_BIT) &
                _server.Caps.HasFlag(DebuggerCapabilities.CAPS_SAVE_CONTEXT_BIT))
            {
                var dbgCtxData = WaitForCommand(new DebugCommand_CtxRead());
                var dbCtxAddr = (dbgCtxData[0] << 8) + dbgCtxData[1];
                // Set the registers from the saved context
                for (var i = 0; i < 32; ++i)
                    ramData[i] = ramData[dbCtxAddr + i];
                // Save the PC address
                _pc = (uint) (ramData[dbCtxAddr + 35] << 8) + ramData[dbCtxAddr + 34];
                _debugTarget.ScriptInterface.CalcValue($"$pc=0x{_pc * 2:X}");
            }
            else
            {
                _pc = (uint) _debugTarget.ScriptInterface.CalcNumericValue("$pc", 0) / 2;
            }
            _debugTarget.Memory.SetMemory(_debugTarget.GetMemType("data"), _ramSpace.Start, 1, ramData.Length, 0, ramData, out status);
            if (_server.Caps.HasFlag(DebuggerCapabilities.CAPS_EEPROM_R_BIT))
            {
                var eepromAddressSpace = _debugTarget.Device.GetAddressSpace("eeprom");
                var eeprom = WaitForCommand(new DebugCommand_EEPROM_Read(0, (uint) eepromAddressSpace.Size));
                _debugTarget.Memory.SetMemory(_debugTarget.GetMemType("eeprom"), eepromAddressSpace.Start, 1,
                    eeprom.Length, 0, eeprom, out status);
            }
            TargetService.MainThreadDispatcher.BeginInvoke(new Action(() =>
            {
                _debugTarget.NotifyTargetBreaked(
                    new DebugTarget.TargetHaltedEventArgs(_pc * 2, "Extern Break",
                        _debugTarget.ProcessesContextid,
                        sEmptyContextData)
                );
                _state = State.InDebug;
                DebugStateChanged?.Invoke();
            }), DispatcherPriority.Background, Array.Empty<object>());
            // Set the pc location for the simulator
        }

        protected byte[] WaitForCommand(IDebugCommand command)
        {
            var task = _server.AddCommand(command);
            task.Wait();
            return task.Result;
        }

        private void DebugWrite(string message)
        {
            _output.DebugOutLine(message);
        }

        private enum State
        {
            None,
            Continue,
            Step,
            InDebug
        }
    }
}