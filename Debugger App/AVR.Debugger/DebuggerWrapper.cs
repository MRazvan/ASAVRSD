using Debugger.Server;
using Debugger.Server.Transports;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Debugger.Server.Commands;

namespace AVR.Debugger
{
    public class DebuggerWrapper : IDebuggerWrapper
    {
        private AvrAddrToLine _addr2Line;
        private string _file;
        private DebugServer _debugServer;
        private ITransport _transport;
        private Dictionary<Events, List<Action>> _eventHandlers;
        private List<Action<byte>> _unknownDataHandlers;
        private Symbol _debugCtxSymbol;

        public CpuState CpuState { get; set; }
        public string Disassembly { get; set; }
        public List<string> SourceFiles { get; set; }
        public List<Symbol> Symbols { get; set; }
        public bool InDebug => _debugServer.InDebug;
        public DebuggerCapabilities Caps => _debugServer.Caps;
        public uint Signature => _debugServer.DeviceSignature;
        public int Version => _debugServer.DebugVersion;

        public LineInfo GetLineFromAddr(uint addr)
        {
            if (string.IsNullOrWhiteSpace(_file) || !File.Exists(_file))
                return null;
            return _addr2Line.GetLineInfo(addr, _file);
        }

        public DebuggerWrapper()
        {
            _debugServer = new DebugServer();
            _transport = new SerialTransport();
            _addr2Line = new AvrAddrToLine();
            _eventHandlers = new Dictionary<Events, List<Action>>();
            _unknownDataHandlers = new List<Action<byte>>();
            CpuState = new CpuState
            {
                Registers = new byte[32]
            };
        }

        public void Load(string file)
        {
            if (File.Exists(file))
            {
                Disassembly = new AvrDisassembler().Disassemble(file);
                Symbols = new AvrNm().GetInfo(file);
                SourceFiles = Symbols.Where(s => !string.IsNullOrWhiteSpace(s.File)).Select(s => s.File).ToList();
                if (_debugCtxSymbol == null)
                    _debugCtxSymbol = Symbols.FirstOrDefault(s => s.Name == "dbg_ctx");
            }
        }

        public void Connect(string port, int speed)
        {
            FireEvent(Events.Starting);
            _debugServer.DebuggerAttached += _debugServer_DebuggerAttached;
            _debugServer.DebuggerDetached += _debugServer_DebuggerDetached;
            _debugServer.DebuggerDisconnected += _debugServer_DebuggerDisconnected;
            _debugServer.UnknownData += _debugServer_UnknownData;
            _transport.SetPort(port);
            _transport.SetSpeed(speed);
            _debugServer.SetTransport(_transport);
            _debugServer.Start();
            FireEvent(Events.Started);
        }

        public void Step()
        {
            if (_debugServer.InDebug)
                _debugServer.Step();
        }

        public void Continue()
        {
            if (_debugServer.InDebug)
                _debugServer.Continue();
        }

        public void Stop()
        {
            if (_debugServer.InDebug)
            {
                FireEvent(Events.Stopping);
                _debugServer.Stop();
                FireEvent(Events.Stopped);
            }
        }


        public void AddEventHandler(Events key, Action action)
        {
            if (!_eventHandlers.ContainsKey(key))
                _eventHandlers[key] = new List<Action>();

            _eventHandlers[key].Add(action);
        }

        public void AddUnknownDataHandler(Action<byte> action)
        {
            _unknownDataHandlers.Add(action);
        }

        private void _debugServer_UnknownData(byte data)
        {
            _unknownDataHandlers.ForEach(a => a.Invoke(data));
        }

        private void _debugServer_DebuggerDisconnected()
        {
            FireEvent(Events.Stopping);
            _debugServer.Stop();
            FireEvent(Events.Stopped);
        }

        private void _debugServer_DebuggerDetached()
        {
            FireEvent(Events.DebugLeave);
        }

        private void _debugServer_DebuggerAttached()
        {
            FireEvent(Events.BeforeDebugEnter);
            if (_debugCtxSymbol == null && _debugServer.Caps.HasFlag(DebuggerCapabilities.CAPS_DBG_CTX_ADDR_BIT))
            {
                var dbgCxtLocationData = WaitForData(new DebugCommand_CtxRead());

                var location = (dbgCxtLocationData[0] << 8) | (dbgCxtLocationData[1]);
                var size = (dbgCxtLocationData[2] << 8) | (dbgCxtLocationData[3]);
                _debugCtxSymbol = new Symbol { Location = (uint)location, Size = (uint)size };
            }
            if (_debugCtxSymbol != null && _debugServer.Caps.HasFlag(DebuggerCapabilities.CAPS_SAVE_CONTEXT_BIT))
            {
                var ramdData = WaitForData(new DebugCommand_Ram_Read(_debugCtxSymbol.Location, _debugCtxSymbol.Size));
                Array.Copy(ramdData, 0, CpuState.Registers, 0, 32);
                CpuState.PC = (uint)((ramdData[35] << 8) | (ramdData[34]));
                CpuState.Stack = (uint)((ramdData[33] << 8) | (ramdData[32]));
            }
            FireEvent(Events.DebugEnter);
        }

        private void FireEvent(Events key)
        {
            if (!_eventHandlers.ContainsKey(key))
                return;

            var handlers = _eventHandlers[key];
            foreach (var handler in handlers)
            {
                handler.Invoke();
            }
        }

        private byte[] WaitForData(IDebugCommand cmd)
        {
            var cmdTask = _debugServer.AddCommand(cmd);
            cmdTask.Wait();
            return cmdTask.Result;
        }

    }
}
