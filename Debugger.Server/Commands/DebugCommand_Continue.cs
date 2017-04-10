namespace Debugger.Server.Commands
{
    public class DebugCommand_Continue : IDebugCommand
    {
        public byte[] CommandBuffer { get; }
            = new byte[1] {DebuggerCommandCodes.DEBUG_REQ_CONTINUE};

        public uint ResponseSize { get; }
            = 0;
    }
}