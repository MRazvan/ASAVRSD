using System;
using System.Collections.Generic;
using AVR.Debugger.Interfaces.Models;
using Debugger.Server;
using ELFSharp.DWARF;
using ELFSharp.ELF;
using LineInfo = AVR.Debugger.Interfaces.Models.LineInfo;

namespace AVR.Debugger.Interfaces
{
    public interface IDebuggerWrapper
    {
        IELF ElfFile { get; }
        DWARFData Dwarf { get; }
        CpuState CpuState { get; }
        List<string> SourceFiles { get; }
        bool InDebug { get; }
        DebuggerCapabilities Caps { get; }
        uint Signature { get; }
        int Version { get; }
        Device Device { get; }
        LineInfo GetLineFromAddr(uint addr);
        void Step();
        void Continue();
        void Stop();
    }
}