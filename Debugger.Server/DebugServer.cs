using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Debugger.Server.Commands;
using Debugger.Server.Transports;

namespace Debugger.Server
{
    public delegate void DebuggerAttachedDelegate();

    public delegate void DebuggerDetachedDelegate();

    public delegate void DebuggerDisconnectedDelegate();

    public delegate void UnknowDataDelegate(byte data);

    public class DebugServer : IDebugServer
    {
        private readonly ConcurrentQueue<DebugCommandWrapper> _commands = new ConcurrentQueue<DebugCommandWrapper>();
        private readonly BackgroundWorker _commandsWorker;

        private readonly byte[] _debugPreambleBuffer = new byte[12];

        private readonly List<DebugPreamble> _debugPreambleData = new List<DebugPreamble>
        {
            new DebugPreamble {Value = 0x21, Action = DebugDetectAction.Compare},
            new DebugPreamble {Value = 0xFE, Action = DebugDetectAction.Compare},
            new DebugPreamble {Value = 0xA9, Action = DebugDetectAction.Compare},
            new DebugPreamble {Value = 0x15, Action = DebugDetectAction.Compare},
            new DebugPreamble {Value = 0x84, Action = DebugDetectAction.Compare},
            new DebugPreamble {Value = 0x00, Action = DebugDetectAction.Skip},
            new DebugPreamble {Value = 0x00, Action = DebugDetectAction.Skip},
            new DebugPreamble {Value = 0x00, Action = DebugDetectAction.Skip},
            new DebugPreamble {Value = 0x00, Action = DebugDetectAction.Skip},
            new DebugPreamble {Value = 0x00, Action = DebugDetectAction.Skip},
            new DebugPreamble {Value = 0x00, Action = DebugDetectAction.Skip},
            new DebugPreamble {Value = 0xFF, Action = DebugDetectAction.Compare}
        };

        private readonly byte[] _emptyCommandResponse = new byte[0];
        private readonly BackgroundWorker _receiverWorker;
        private DebugCommandWrapper _currentCommand;
        private byte[] _currentCommandBuffer;
        private int _currentCommandReceiveIdx;
        private int _debugPreambleIdx;
        private DebuggerState _state;
        private ITransport _transport;

        public DebugServer()
        {
            InDebug = false;
            Caps = DebuggerCapabilities.CAPS_RAM_R_BIT;
            DeviceSignature = 0x0;
            DebugVersion = 0x0;
            _debugPreambleIdx = 0;
            _state = DebuggerState.NotConnected;
            _receiverWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _receiverWorker.DoWork += _receiverWorker_DoWork;

            _commandsWorker = new BackgroundWorker {WorkerSupportsCancellation = true};
            _commandsWorker.DoWork += _commandsWorker_DoWork;
        }

        public bool InDebug { get; set; }
        public DebuggerCapabilities Caps { get; set; }
        public byte DebugVersion { get; set; }
        public uint DeviceSignature { get; set; }

        public event DebuggerDisconnectedDelegate DebuggerDisconnected;
        public event DebuggerAttachedDelegate DebuggerAttached;
        public event DebuggerDetachedDelegate DebuggerDetached;
        public event UnknowDataDelegate UnknownData;

        public void Continue()
        {
            ExecuteCommandWithClearState(new DebugCommand_Continue());
        }

        public void Start()
        {
            _transport.Connect();
            _receiverWorker.RunWorkerAsync();
            _commandsWorker.RunWorkerAsync();
        }

        public Task<byte[]> AddCommand(IDebugCommand cmd)
        {
            if (_state == DebuggerState.NotConnected || _state == DebuggerState.Stopping)
                return Task.FromResult<byte[]>(null);

            var wrapper = new DebugCommandWrapper(cmd);
            _commands.Enqueue(wrapper);
            return wrapper.TCS.Task;
        }

        public void Stop()
        {
            InDebug = false;
            _state = DebuggerState.Stopping;
            _commandsWorker.CancelAsync();
            _receiverWorker.CancelAsync();
            _transport.Disconnect();
            _state = DebuggerState.NotConnected;
        }

        public void SetTransport(ITransport transport)
        {
            _transport = transport;
        }

        public void Step()
        {
            ExecuteCommandWithClearState(new DebugCommand_Step());
        }

        private void ExecuteCommandWithClearState(IDebugCommand command)
        {
            InDebug = false;

            // Clear the commands buffer
            DebugCommandWrapper cmd;
            while (_commands.TryDequeue(out cmd))
                cmd.TCS.SetResult(_emptyCommandResponse);
            // Clear the current command
            _currentCommand = null;
            _currentCommandBuffer = null;
            _currentCommandReceiveIdx = 0;

            _state = DebuggerState.NotConnected;
            // Notify anyone interested
            DebuggerDetached?.Invoke();

            _transport.Write(command.CommandBuffer);
        }

        public void ResetTarget()
        {
            _transport?.ResetTarget();
        }

        private void _commandsWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_commandsWorker.CancellationPending)
            {
                DebugCommandWrapper tempCommand = null;
                if (_currentCommand == null && _commands.TryDequeue(out tempCommand))
                {
                    // Carefully when changing this,
                    //  We have the receive task that is running async
                    if (tempCommand.Command.ResponseSize == 0)
                    {
                        _transport.Write(tempCommand.Command.CommandBuffer);
                        tempCommand.TCS.SetResult(_emptyCommandResponse);
                        continue; // Try the next command, do not wait
                    }

                    _currentCommandBuffer = new byte[tempCommand.Command.ResponseSize];
                    _currentCommandReceiveIdx = 0;
                    _currentCommand = tempCommand;
                    _transport.Write(_currentCommand.Command.CommandBuffer);
                }

                // Delay a bit so we don't block the CPU
                Thread.Sleep(10);
            }
        }

        private void DettectDebugRequest(byte data)
        {
            if (_state != DebuggerState.NotConnected) return;

            _debugPreambleBuffer[_debugPreambleIdx] = data;

            if (_debugPreambleData[_debugPreambleIdx].Action == DebugDetectAction.Skip)
            {
                _debugPreambleIdx++;
                return;
            }
            if (_debugPreambleData[_debugPreambleIdx].Value != data)
            {
                // If we have data and we could not match the preamble
                //  Dump the data (basically notify anyone else interested in the data)
                for (var i = 0; i <= _debugPreambleIdx; ++i)
                    UnknownData?.Invoke(_debugPreambleBuffer[i]);
                _debugPreambleIdx = 0;
                return;
            }

            _debugPreambleIdx++;
            if (_debugPreambleIdx != _debugPreambleData.Count)
                return;

            _state = DebuggerState.Connected;
            DebugVersion = _debugPreambleBuffer[5];
            DeviceSignature = (uint) (
                (_debugPreambleBuffer[6] << 16) +
                (_debugPreambleBuffer[7] << 8) +
                _debugPreambleBuffer[8]
            );
            Caps = (DebuggerCapabilities) (
                (_debugPreambleBuffer[10] << 8) +
                _debugPreambleBuffer[9]
            );
            _debugPreambleIdx = 0;
        }

        private void ProcessData(byte data)
        {
            if (_state == DebuggerState.NotConnected)
            {
                DettectDebugRequest(data);
                if (_state != DebuggerState.NotConnected)
                {
                    InDebug = true;
                    Task.Run(() => { DebuggerAttached?.Invoke(); });
                }
            }
            else
            {
                if (_currentCommand != null)
                {
                    _currentCommandBuffer[_currentCommandReceiveIdx++] = data;
                    if (_currentCommandReceiveIdx != _currentCommand.Command.ResponseSize)
                        return;
                    _currentCommand.TCS.SetResult(_currentCommandBuffer);
                    _currentCommand = null;
                }
                else
                {
                    UnknownData?.Invoke(data);
                }
            }
        }

        private void _receiverWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (!_receiverWorker.CancellationPending)
                {
                    var data = _transport.ReadByte();
                    if (_state != DebuggerState.Stopping)
                        ProcessData(data);
                }
            }
            catch (IOException)
            {
                if (_state == DebuggerState.Stopping)
                {
                    // We are stopping we closed the transport layer
                }
                else
                {
                    // ?? do what? should we just throw and let others
                    //  handle this?
                    throw;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // In case of serial transport
                //  this happens when the port becommes unavailable
                //  usually when the cable was disconnected
                // Should we notifiy someone?
                Task.Run(() => { DebuggerDisconnected?.Invoke(); });
            }
        }
    }
}