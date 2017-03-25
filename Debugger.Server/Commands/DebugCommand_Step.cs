using System;

namespace Debugger.Server.Commands
{
    public class DebugCommand_Step : IDebugCommand
    {
        public byte[] CommandBuffer { get; private set; }
            = new byte[1] { DebuggerCommandCodes.DEBUG_REQ_SINGLE_STEP };
        public uint ResponseSize { get; private set; }
            = 0;
    }
}
