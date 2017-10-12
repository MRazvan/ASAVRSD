using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AVR.Debugger.Interfaces;
using AVR.Debugger.Interfaces.Models;
using AVR.Debugger.Tools;
using Debugger.Server;
using Debugger.Server.Commands;
using Debugger.Server.Transports;
using ELFSharp.DWARF;
using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using LineInfo = AVR.Debugger.Interfaces.Models.LineInfo;

namespace AVR.Debugger
{
    public class DebuggerWrapper : IDebuggerWrapper
    {
        public DWARFData Dwarf { get; private set; }
        public ELF<uint> ElfFile { get; private set; }

        private Symbol _debugCtxSymbol;
        private readonly DebugServer _debugServer;
        private readonly Dictionary<Events, List<Action>> _eventHandlers;
        private string _file;
        private readonly ISynchronizeInvoke _syncContext;
        private readonly ITransport _transport;

        private readonly List<Action<byte>> _unknownDataHandlers;


        public DebuggerWrapper(ISynchronizeInvoke syncContext)
        {
            _syncContext = syncContext;
            _debugServer = new DebugServer();
            _transport = new SerialTransport();
            _eventHandlers = new Dictionary<Events, List<Action>>();
            _unknownDataHandlers = new List<Action<byte>>();
            CpuState = new CpuState
            {
                Registers = new byte[32]
            };
        }

        public CpuState CpuState { get; set; }
        public string Disassembly { get; set; }
        public List<string> SourceFiles { get; set; }
        public bool InDebug => _debugServer.InDebug;
        public DebuggerCapabilities Caps => _debugServer.Caps;
        public uint Signature => _debugServer.DeviceSignature;
        public int Version => _debugServer.DebugVersion;

        public LineInfo GetLineFromAddr(uint addr)
        {
            var li = Dwarf.LineSection.GetLineFromAddress(addr);
            if (li != null)
                return new LineInfo {File = SourceFiles.First(s => s.EndsWith(li.File.File)), Line = (int) li.Line};
            return null;
        }

        public void Load(string file)
        {
            if (File.Exists(file))
            {
                _file = file;
                ElfFile = ELFReader.Load<uint>(_file);
                Dwarf = new DWARFData(ElfFile);
                Disassembly = new AvrDisassembler().Disassemble(file);
                SourceFiles = new List<string>();
                Dwarf.LineSection.GetFiles().ForEach(f =>
                {
                    var path = Path.Combine(f.Directory, f.File);
                    if (path.StartsWith("/") || path.StartsWith("..") || path.StartsWith("\\"))
                        path = Path.Combine(Path.GetDirectoryName(_file), path);
                    SourceFiles.Add(Path.GetFullPath(path));
                });

                var symbols = ((ISymbolTable) ElfFile.GetSection(".symtab")).Entries.Where(x => x.Type == SymbolType.Object);
                if (_debugCtxSymbol == null)
                {
                    var entry = symbols.FirstOrDefault(s => s.Name == "dbg_context");
                    if (entry != null)
                    {
                        _debugCtxSymbol = new Symbol
                        {
                            Size = (uint) entry.Size,
                            Location = (uint) (entry.Value - 0x800000)
                        };
                    }
                }
            }
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
            _debugServer.ResetTarget();
            FireEvent(Events.Started);
        }

        private void _debugServer_UnknownData(byte data)
        {
            _unknownDataHandlers.ForEach(handler =>
            {
                if (_syncContext.InvokeRequired)
                    _syncContext.BeginInvoke(handler, new object[] {data});
                else
                    handler.Invoke(data);
            });
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

                var location = (dbgCxtLocationData[0] << 8) | dbgCxtLocationData[1];
                var size = (dbgCxtLocationData[2] << 8) | dbgCxtLocationData[3];
                _debugCtxSymbol = new Symbol {Location = (uint) location, Size = (uint) size};
            }
            if (_debugCtxSymbol != null && _debugServer.Caps.HasFlag(DebuggerCapabilities.CAPS_SAVE_CONTEXT_BIT))
            {
                var ramdData = WaitForData(new DebugCommand_Ram_Read(_debugCtxSymbol.Location, _debugCtxSymbol.Size));
                Array.Copy(ramdData, 0, CpuState.Registers, 0, 32);
                CpuState.PC = (uint) (((ramdData[35] << 8) | ramdData[34]) * 2);
                CpuState.Stack = (uint) ((ramdData[33] << 8) | ramdData[32]);
            }
            FireEvent(Events.DebugEnter);
            FireEvent(Events.AfterDebugEnter);
        }

        private void FireEvent(Events key)
        {
            Debug.WriteLine("Fire Event " + key);
            if (!_eventHandlers.ContainsKey(key))
                return;

            var handlers = _eventHandlers[key];
            foreach (var handler in handlers)
                if (_syncContext.InvokeRequired)
                    _syncContext.BeginInvoke(handler, null);
                else
                    handler.Invoke();
        }

        private byte[] WaitForData(IDebugCommand cmd)
        {
            var cmdTask = _debugServer.AddCommand(cmd);
            cmdTask.Wait();
            return cmdTask.Result;
        }
    }
}