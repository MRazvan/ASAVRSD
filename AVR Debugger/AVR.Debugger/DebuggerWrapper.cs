using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public IELF ElfFile { get; private set; }
        public Device Device {
            get
            {
                if (!_debugServer.InDebug)
                    return null;
                return GetService<IDeviceInfoProvider>()?.GetDevice(_debugServer.DeviceSignature);
            }
        }
        public CpuState CpuState { get; set; }
        public string Disassembly { get; set; }
        public List<string> SourceFiles { get; set; }
        public bool InDebug => _debugServer.InDebug;
        public DebuggerCapabilities Caps => _debugServer.Caps;
        public uint Signature => _debugServer.DeviceSignature;
        public int Version => _debugServer.DebugVersion;
        
        private Symbol _debugCtxSymbol;
        private string _file;
        private readonly DebugServer _debugServer;
        private readonly ITransport _transport;      
        private readonly uint _entryOffset;
        private readonly Interfaces.IServiceProvider _serviceProvider;
        private readonly IEventService _eventService;

        public DebuggerWrapper(Interfaces.IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _entryOffset = 0x800000;
            _debugServer = new DebugServer();
            _transport = new SerialTransport();
            _eventService = serviceProvider.GetService<IEventService>();
            CpuState = new CpuState
            {
                Registers = new byte[32]
            };
        }



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
                ElfFile = ELFReader.Load(_file);
                Dwarf = new DWARFData(ElfFile);
                Disassembly = new AvrDisassembler().Disassemble(file);
                SourceFiles = GetSourceFiles(Dwarf);
                _debugCtxSymbol = LoadDebugContextInfo(ElfFile);
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
                FireEvent(Events.Debug_Stopping);
                _debugServer.Stop();
                FireEvent(Events.Debug_Stopped);
            }
        }

        public void Connect(string port, int speed)
        {
            if (_debugServer.InDebug)
                return;
            FireEvent(Events.Debug_Starting);
            _debugServer.DebuggerAttached += _debugServer_DebuggerAttached;
            _debugServer.DebuggerDetached += _debugServer_DebuggerDetached;
            _debugServer.DebuggerDisconnected += _debugServer_DebuggerDisconnected;
            _debugServer.UnknownData += _debugServer_UnknownData;
            _transport.SetPort(port);
            _transport.SetSpeed(speed);
            _debugServer.SetTransport(_transport);
            _debugServer.Start();
            _debugServer.ResetTarget();
            FireEvent(Events.Debug_Started);
        }

        private void _debugServer_UnknownData(byte data)
        {
            FireEvent(Events.Debug_UnknownData, data);
        }

        private void _debugServer_DebuggerDisconnected()
        {
            FireEvent(Events.Debug_Stopping);
            _debugServer.Stop();
            FireEvent(Events.Debug_Stopped);
        }

        private void _debugServer_DebuggerDetached()
        {
            FireEvent(Events.Debug_Leave);
        }

        private void _debugServer_DebuggerAttached()
        {
            FireEvent(Events.Debug_BeforeEnter);
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
            FireEvent(Events.Debug_Enter);
            FireEvent(Events.Debug_AfterEnter);
        }

        private void FireEvent(Events key, object data = null)
        {
            _eventService.FireEvent(key, data);
        }

        private byte[] WaitForData(IDebugCommand cmd)
        {
            var cmdTask = _debugServer.AddCommand(cmd);
            cmdTask.Wait();
            return cmdTask.Result;
        }

        private T GetService<T>()
        {
            return _serviceProvider.GetService<T>();
        }

        private List<string> GetSourceFiles(DWARFData _dwarf)
        {
            var sourceFiles = new List<string>();
            _dwarf.LineSection.GetFiles().ForEach(f =>
            {
                var path = Path.Combine(f.Directory, f.File);
                if (path.StartsWith("/") || path.StartsWith("..") || path.StartsWith("\\"))
                    path = Path.Combine(Path.GetDirectoryName(_file), path);
                sourceFiles.Add(Path.GetFullPath(path));
            });
            return sourceFiles;
        }

        private Symbol LoadDebugContextInfo(IELF elfFile)
        {
            var symbols = ((ISymbolTable)elfFile.GetSection(".symtab")).Entries.Where(x => x.Type == SymbolType.Object);
            if (_debugCtxSymbol == null)
            {
                var entry = symbols.FirstOrDefault(s => s.Name == "dbg_context");
                if (entry != null)
                {
                    return new Symbol
                    {
                        Size = (uint)entry.Size,
                        Location = (uint)(entry.Value - _entryOffset)
                    };
                }
            }
            return null;
        }
    }
}