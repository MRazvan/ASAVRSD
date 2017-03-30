using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Atmel.VsIde.AvrStudio.Services.TargetService;
using Atmel.VsIde.AvrStudio.Services.TargetService.TCF.Protocol;
using Debugger.Server;

namespace Microsoft.WPFWizardExample
{
    public delegate void ContextResumedDelegate();

    public delegate void ContextSuspendedDelegate();

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

        public event ContextResumedDelegate DebugLeave;
        public event ContextSuspendedDelegate DebugEnter;
        public event MemoryChangedDelegate MemoryChanged;

        private bool _updateSuspended;
        private State _state;
        private readonly IDebugServer _server;
        private readonly CountdownEvent _suspendBarrier;
        private readonly Thread _notifyDebugEnterThread;

        public DebuggerEventsProxy(IDebugServer server)
        {
            _server = server;
            _suspendBarrier = new CountdownEvent(2);
            _notifyDebugEnterThread = new Thread(HandleNotifyDebugEnter) {Name = "HandleNotifyDebugEnter"};
            _state = State.None;
        }

        public void Start(DebugTarget target)
        {
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
                    return;
                case "contextSuspended":
                    if (_state == State.InDebug)
                        return;
                    _state = State.PartialEnterDebug;
                    _suspendBarrier.Signal();
                    return;
                case "memoryChanged":
                    object[] sequence = JSON.ParseSequence(data);
                    HandleMemoryChanged(sequence);
                    break;
            }
        }

        private void Server_DebuggerAttached()
        {
            if (_state == State.InDebug)
                return;
            _state = State.PartialEnterDebug;
            _suspendBarrier.Signal();
        }

        private void HandleMemoryChanged(object[] sequence)
        {
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
            MemoryChanged?.Invoke(memId, memAddr, memSize);
        }

        private void HandleNotifyDebugEnter()
        {
            while (true)
            {
                _suspendBarrier.Wait();

                switch (_state)
                {
                    case State.Stopping:
                        return;
                    case State.None:
                        _suspendBarrier.Reset();
                        continue;
                }
                _state = State.InDebug;
                DebugEnter?.Invoke();
                _updateSuspended = false;
                _suspendBarrier.Reset();
            }
        }
    }
}
