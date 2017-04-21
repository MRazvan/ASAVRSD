using System;
using System.Collections.Generic;
using AVR.Debugger.Interfaces.Models;
using Debugger.Server;
using ELFSharp.DWARF;
using ELFSharp.ELF;
using LineInfo = AVR.Debugger.Interfaces.Models.LineInfo;

namespace AVR.Debugger.Interfaces
{
    public enum Events
    {
        None,
        Starting,
        Started,
        BeforeDebugEnter,
        DebugEnter,
        AfterDebugEnter,
        DebugLeave,
        Stopping,
        Stopped
    }

    public interface IDebuggerWrapper
    {
        ELF<uint> ElfFile { get; }
        DWARFData Dwarf { get; }
        CpuState CpuState { get; }
        List<string> SourceFiles { get; }
        string Disassembly { get; }
        bool InDebug { get; }
        DebuggerCapabilities Caps { get; }
        uint Signature { get; }
        int Version { get; }
        void AddEventHandler(Events key, Action action);
        void AddUnknownDataHandler(Action<byte> action);
        LineInfo GetLineFromAddr(uint addr);
        void Step();
        void Continue();
        void Stop();
    }
}