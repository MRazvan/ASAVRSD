using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Atmel.VsIde.AvrStudio.Services.TargetService;
using Atmel.VsIde.AvrStudio.Services.TargetService.TCF.Protocol;
using Debugger.Server;
using System.Windows.Threading;
using Microsoft.WPFWizardExample.SDebugger;
using System.Reflection;

namespace Microsoft.WPFWizardExample
{
    public delegate void DebugLeaveDelegate();

    public delegate void DebugEnterDelegate();

    public delegate void MemoryChangedDelegate(string memId, long addr, long size);

    class DebuggerEventsProxy : IEventListener
    {
        enum State
        {
            None,
            Stopping,
            PartialEnterDebug,
            InDebug
        }

        public event DebugLeaveDelegate DebugLeave;
        public event DebugEnterDelegate DebugEnter;
        public event MemoryChangedDelegate MemoryChanged;

        private bool _updateSuspended;
        private State _state;
        private readonly IDebugServer _server;
        private readonly CountdownEvent _suspendBarrier;
        private readonly Thread _notifyDebugEnterThread;
        private Output _output;

        public bool InDebug => _state == State.InDebug;

        public DebuggerEventsProxy(IDebugServer server, Output output)
        {
            _output = output;
            _server = server;
            _suspendBarrier = new CountdownEvent(2);
            _notifyDebugEnterThread = new Thread(HandleNotifyDebugEnter) {Name = "HandleNotifyDebugEnter"};
            _state = State.None;
        }

        public void Start(DebugTarget target)
        {
            _updateSuspended = true;
            var channel = target.KnownTool.GetChannel();
            var services = channel.GetRemoteServices().ToList();
            services.ForEach(service => channel.AddEventListener(service, this));

            _suspendBarrier.Reset();
            _notifyDebugEnterThread.Start();
            _server.DebuggerAttached += Server_DebuggerAttached;
        }

        public void Stop(DebugTarget target)
        {
            var channel = target.KnownTool.GetChannel();
            var services = channel.GetRemoteServices().ToList();
            services.ForEach(service => channel.RemoveEventListener(service, this));

            _state = State.Stopping;
            _server.DebuggerAttached -= Server_DebuggerAttached;

            _suspendBarrier.Reset();
            _suspendBarrier.Signal(2);
            _notifyDebugEnterThread.Join();
            _state = State.None;
        }

        public void Event(string name, byte[] data)
        {
            Write(MethodBase.GetCurrentMethod().Name + $"   {name}");
            switch (name)
            {
                case "contextResumed":
                    if (_state == State.PartialEnterDebug)
                    {
                        _state = State.None;
                        _suspendBarrier.Signal(_suspendBarrier.CurrentCount);
                    }

                    _state = State.None;
                    _updateSuspended = true;
                    DebugLeave?.Invoke();
                    break;
                case "contextSuspended":
                    if (_state == State.InDebug)
                        break;
                    _state = State.PartialEnterDebug;
                    _suspendBarrier.Signal();
                    break;
                case "memoryChanged":
                    object[] sequence = JSON.ParseSequence(data);
                    HandleMemoryChanged(sequence);
                    if (_state == State.PartialEnterDebug)
                        _state = State.InDebug;
                    break;
            }
        }

        private void Server_DebuggerAttached()
        {
            Write(MethodBase.GetCurrentMethod().Name);
            if (_state == State.InDebug)
                return;
            _state = State.PartialEnterDebug;
            _suspendBarrier.Signal();
        }

        private void HandleMemoryChanged(object[] sequence)
        {
            Write(MethodBase.GetCurrentMethod().Name);
            if (_updateSuspended)
                return;

            if (_state != State.InDebug)
                return;

            if (sequence.Length < 2)
                return;

            var memId = sequence[0] as string;
            if (string.IsNullOrWhiteSpace(memId))
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

            var memAddr = (long)properties["addr"];
            var memSize = (long)properties["size"];
            Write(MethodBase.GetCurrentMethod().Name + " - Memory Changed");
            MemoryChanged?.Invoke(memId, memAddr, memSize);
        }

        private void HandleNotifyDebugEnter()
        {
            while (true)
            {
                _suspendBarrier.Wait();
                _suspendBarrier.Reset();
                Write(MethodBase.GetCurrentMethod().Name);
                switch (_state)
                {
                    case State.Stopping:
                        return;
                    case State.None:
                        continue;
                }
                Write(MethodBase.GetCurrentMethod().Name);
                DebugEnter?.Invoke();
                _updateSuspended = false;
            }
        }

        private void Write(string message)
        {
            _output.TraceOut(message + $"  - {_suspendBarrier.CurrentCount} - {_state}\n");
        }
    }
}
