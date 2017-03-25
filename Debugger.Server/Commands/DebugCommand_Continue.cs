namespace Debugger.Server.Commands
{
    public class DebugCommand_Continue : IDebugCommand
    {
        public byte[] CommandBuffer { get; private set; }
            = new byte[1] { DebuggerCommandCodes.DEBUG_REQ_CONTINUE };
        public uint ResponseSize { get; private set; } 
            = 0;
    }
}
