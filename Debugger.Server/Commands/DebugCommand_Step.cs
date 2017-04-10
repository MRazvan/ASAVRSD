namespace Debugger.Server.Commands
{
    public class DebugCommand_Step : IDebugCommand
    {
        public byte[] CommandBuffer { get; }
            = new byte[1] {DebuggerCommandCodes.DEBUG_REQ_SINGLE_STEP};

        public uint ResponseSize { get; }
            = 0;
    }
}