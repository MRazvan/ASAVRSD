using Debugger.Server;
using System;
using System.Collections.Generic;

namespace AVR.Debugger
{
    public enum Events
    {
        None,
        Starting,
        Started,
        BeforeDebugEnter,
        DebugEnter,
        DebugLeave,
        Stopping,
        Stopped
    };

    interface IDebuggerWrapper
    {
        void AddEventHandler(Events key, Action action);
        void AddUnknownDataHandler(Action<byte> action);
        CpuState CpuState { get; }
        LineInfo GetLineFromAddr(uint addr);
        List<string> SourceFiles { get; }
        string Disassembly { get; }
        List<Symbol> Symbols { get; }
        bool InDebug { get; }
        DebuggerCapabilities Caps { get; }
        uint Signature { get; }
        int Version { get; }

        void Load(string file);

        void Step();
        void Continue();
        void Stop();
    }
}
