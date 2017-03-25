using System;
using System.Collections.Generic;
using System.Linq;
using Atmel.Studio.Services;
using Atmel.Studio.Services.Device;
using Atmel.VsIde.AvrStudio.Services.TargetService;
using Atmel.VsIde.AvrStudio.Services.TargetService.TCF.Core;
using Debugger.Server;
using Debugger.Server.Commands;
using Microsoft.VisualStudio.Shell.Interop;
using IServiceProvider = System.IServiceProvider;
using System.Windows.Threading;

namespace Microsoft.WPFWizardExample.SDebugger
{
    public class SimulatorDebugger : IVsDebuggerEvents
    {
        public static Guid SDSerialOutputPane = Guid.Parse("{9F79FB17-B312-4050-90D4-A90D335ABFD8}");
        public static Guid SDTraceOutputPane = Guid.Parse("{1FFE1F93-F2A8-4BC6-B83B-B88E6DD52FD8}");

        
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsOutputWindowPane _serialOutPane;
        private readonly IVsOutputWindowPane _traceOutPane;

        private DebugTarget _debugTarget;
        private uint _cookie;
        private uint _pc;
        private IAddressSpace _ramSpace;
        private bool _running;
        private DebugServer _server;
        private ITargetService _target;
        private EventListener _eventListener;
        private AbstractChannel _channel;
        private List<string> _services;

        private Action _contextSuspendedAction;
        private Action _contextResumedAction;
        private bool _step;

        public SimulatorDebugger(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _running = false;
            _serialOutPane = InitializePane(ref SDSerialOutputPane, "SD Serial Output", true, true);
            _traceOutPane = InitializePane(ref SDTraceOutputPane, "SD Trace Output", true, true);
        }

        public int OnModeChange(DBGMODE dbgmodeNew)
        {
            switch (dbgmodeNew)
            {
                case DBGMODE.DBGMODE_Break:
                    DbgBreak();
                    break;
                case DBGMODE.DBGMODE_Design:
                    DbgStop();
                    break;
                case DBGMODE.DBGMODE_Run:
                    DbgRun();
                    break;
            }
            return 0;
        }

        public void Start(ITransport transport)
        {
            if (_running)
                return;

            // Make the serial out active
            _serialOutPane.Activate();

            _target = ATServiceProvider.GetService<STargetService, ITargetService>();
           
            _debugTarget = _target.GetCurrentTarget();
            _pc = 0;
            _ramSpace = _debugTarget.Device.GetAddressSpace("data");

            // Start the debug server
            _server = new DebugServer(transport);
            _server.DebuggerAttached += ServerOnDebuggerAttached;
            _server.UnknownData += SerialOut;
            _server.Start();

            // Setup the debug events handler
            var vsDebugger = _serviceProvider.GetService(typeof(IVsDebugger)) as IVsDebugger;
            vsDebugger?.AdviseDebuggerEvents(this, out _cookie);

            _eventListener = new EventListener(_debugTarget, _server, _debugTarget.GetMemType("data"));
            _eventListener.ContextSuspended = ContextSuspended;
            _eventListener.ContextResumed = ContextResumed;

            _channel = _debugTarget.KnownTool.GetChannel();
            _services = _channel.GetRemoteServices().ToList();
            _services.ForEach(service => _channel.AddEventListener(service, _eventListener));

            _running = true;
        }

        private void ContextResumed()
        {
            if (_contextResumedAction == null)
                return;
            TargetService.MainThreadDispatcher.BeginInvoke(new Action(() => {
                _contextResumedAction();
                _contextResumedAction = null;
            }), DispatcherPriority.Background, null);
        }

        private void ContextSuspended()
        {
            if (_contextSuspendedAction == null)
                return;
            TargetService.MainThreadDispatcher.BeginInvoke(new Action(() => {
                _contextSuspendedAction();
                _contextSuspendedAction = null;
            }), DispatcherPriority.Background, null);
        }

        public void Stop()
        {
            if (!_running)
                return;

            _server.Stop();
            _server.DebuggerAttached -= ServerOnDebuggerAttached;
            _server.UnknownData -= SerialOut;
            _server = null;

            _target = null;
            _debugTarget = null;
            _ramSpace = null;
            _pc = 0;

            var vsDebugger = _serviceProvider.GetService(typeof(IVsDebugger)) as IVsDebugger;
            vsDebugger?.UnadviseDebuggerEvents(_cookie);
            _cookie = 0;

            _services.ForEach(service => _channel.RemoveEventListener(service, _eventListener));
            _running = false;
        }

        public void Step()
        {
            if (_server.InDebug && _contextResumedAction == null)
            {
                _traceOutPane.OutputString("STEP init\n");
                _step = true;
                _contextResumedAction = () =>
                {
                    // Tell the debug server to send the continue command
                    _server.Step();
                    _traceOutPane.OutputString("STEP done\n");
                };
                // We don't want to update the RAM in run mode
                _eventListener.SuspendUpdate();

                IStatus status;
                _debugTarget.SDM_Step(1, out status);
            }
        }

        private async void ServerOnDebuggerAttached()
        {
            // Check if the signature matches
            if (_debugTarget.Device.Signature != _server.DeviceSignature)
            {
                _debugTarget.ScriptInterface.DisplayDialogBox("Error", $"Mismatch signatures.\nFound 0x{_server.DeviceSignature} expected {_debugTarget.Device.Signature}\nDetaching...", (int)DialogIcon.Error);
                DbgStop();
                return;
            }

            IStatus status;
            var ramData = await _server.AddCommand(new DebugCommand_Ram_Read((uint)_ramSpace.Start, (uint)_ramSpace.Size));
            if (_server.Caps.HasFlag(DebuggerCapabilities.CAPS_DBG_CTX_ADDR_BIT) &
                _server.Caps.HasFlag(DebuggerCapabilities.CAPS_SAVE_CONTEXT_BIT))
            {
                var dbgCtxData = await _server.AddCommand(new DebugCommand_CtxRead());
                var dbCtxAddr = (dbgCtxData[0] << 8) + dbgCtxData[1];
                // Set the registers from the saved context
                for (var i = 0; i < 32; ++i)
                    ramData[i] = ramData[dbCtxAddr + i];
                // Save the PC address
                _pc = (uint)(ramData[dbCtxAddr + 35] << 8) + ramData[dbCtxAddr + 34];

                _contextSuspendedAction = () =>
                {
                    // Setup the ram in the simulator with our hardware ram
                    _debugTarget.Memory.SetMemory(_debugTarget.GetMemType("data"), _ramSpace.Start, 1, ramData.Length, 0, ramData, out status);
                    // Set the pc location for the simulator
                    _debugTarget.ScriptInterface.CalcValue($"$pc=0x{_pc * 2:X}");
                    _debugTarget.NotifyTargetBreaked(
                        new DebugTarget.TargetHaltedEventArgs(_pc * 2, "Extern Break", _debugTarget.ProcessesContextid, null)
                    );
                    _eventListener.ResumeUpdate();

                };
            }
            else
            {
                _contextSuspendedAction = () =>
                {
                    // Setup the ram in the simulator with our copy
                    _debugTarget.Memory.SetMemory(_debugTarget.GetMemType("data"), _ramSpace.Start, 1, ramData.Length, 0, ramData, out status);
                    _debugTarget.NotifyTargetBreaked(
                        new DebugTarget.TargetHaltedEventArgs(_pc * 2, "Extern Break", _debugTarget.ProcessesContextid, null)
                    );
                    _eventListener.ResumeUpdate();
                };
            }


            _eventListener.SuspendUpdate();
            if (!_step)
            {
                _traceOutPane.OutputString("Continue\n");
                // Suspend the target so we can update the state
                _debugTarget.Suspend(out status);
            }else
            {
                _step = false;
                _traceOutPane.OutputString("STEP debugger\n");
            }
        }

        private void DbgRun()
        {
            if (_server.InDebug && _running)
            {
                // We don't want to update the RAM in run mode
                _eventListener.SuspendUpdate();
                // Tell the debug server to send the continue command
                _server.Continue();
            }
        }

        private void DbgStop()
        {
            if (_running)
                Stop();
        }

        private void DbgBreak()
        {
        }

        private IVsOutputWindowPane InitializePane(ref Guid id, string title, bool visible, bool clearWithSolution)
        {
            var output = (IVsOutputWindow)_serviceProvider.GetService(typeof(SVsOutputWindow));
            IVsOutputWindowPane pane;
            output.GetPane(ref id, out pane);
            if (pane == null)
            {
                // Create a new pane.  
                output.CreatePane(
                    ref id,
                    title,
                    Convert.ToInt32(visible),
                    Convert.ToInt32(clearWithSolution));
                // Retrieve the new pane.  
                output.GetPane(ref id, out pane);
            }
            return pane;
        }

        private void SerialOut(byte data)
        {
            _serialOutPane?.OutputString(Convert.ToChar(data) + "");
        }

        private void TraceOut(string message)
        {
            _traceOutPane?.OutputString(message);
        }
    }
}