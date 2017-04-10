using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;
using Atmel.VsIde.AvrStudio.Services.TargetService;
using Atmel.VsIde.AvrStudio.Services.TargetService.TCF.Protocol;
using Debugger.Server;

namespace SoftwareDebuggerExtension.SDebugger
{
    public delegate void DebugLeaveDelegate();

    public delegate void DebugEnterDelegate();

    public delegate void MemoryChangedDelegate(string memId, long addr, long size);

    internal class DebuggerEventsProxy : IEventListener
    {
        private readonly IDebugServer _server;
        private readonly CountdownEvent _suspendBarrier;
        private Thread _notifyDebugEnterThread;
        private readonly Output _output;
        private State _state;

        private bool _updateSuspended;

        public DebuggerEventsProxy(IDebugServer server, Output output)
        {
            _output = output;
            _server = server;
            _suspendBarrier = new CountdownEvent(2);
            _state = State.None;
        }

        public bool InDebug => _state == State.InDebug;

        public void Event(string name, byte[] data)
        {
            DebugWrite(MethodBase.GetCurrentMethod().Name + $"   {name}");
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
                    var sequence = JSON.ParseSequence(data);
                    HandleMemoryChanged(sequence);
                    if (_state == State.PartialEnterDebug)
                        _state = State.InDebug;
                    break;
            }
        }

        public event DebugLeaveDelegate DebugLeave;
        public event DebugEnterDelegate DebugEnter;
        public event MemoryChangedDelegate MemoryChanged;

        public void Start(DebugTarget target)
        {
            _updateSuspended = true;
            var channel = target.KnownTool.GetChannel();
            var services = channel.GetRemoteServices().ToList();
            services.ForEach(service => channel.AddEventListener(service, this));

            _suspendBarrier.Reset();

            _notifyDebugEnterThread = new Thread(HandleNotifyDebugEnter) {Name = "HandleNotifyDebugEnter"};
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

        private void Server_DebuggerAttached()
        {
            DebugWrite(MethodBase.GetCurrentMethod().Name);
            if (_state == State.InDebug)
                return;
            _state = State.PartialEnterDebug;
            _suspendBarrier.Signal();
        }

        private void HandleMemoryChanged(object[] sequence)
        {
            DebugWrite(MethodBase.GetCurrentMethod().Name);
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

            var memAddr = (long) properties["addr"];
            var memSize = (long) properties["size"];
            DebugWrite(MethodBase.GetCurrentMethod().Name + " - Memory Changed");
            if (MemoryChanged != null)
                TargetService.MainThreadDispatcher.BeginInvoke(MemoryChanged, DispatcherPriority.Background, memId,
                    memAddr, memSize);
        }

        private void HandleNotifyDebugEnter()
        {
            while (true)
            {
                _suspendBarrier.Wait();
                _suspendBarrier.Reset();
                DebugWrite(MethodBase.GetCurrentMethod().Name);
                switch (_state)
                {
                    case State.Stopping:
                        return;
                    case State.None:
                        continue;
                }
                DebugWrite(MethodBase.GetCurrentMethod().Name);
                DebugEnter?.Invoke();
                _updateSuspended = false;
            }
        }

        private void DebugWrite(string message)
        {
            _output.DebugOutLine(message + $"  - {_suspendBarrier.CurrentCount} - {_state}");
        }

        private enum State
        {
            None,
            Stopping,
            PartialEnterDebug,
            InDebug
        }
    }
}