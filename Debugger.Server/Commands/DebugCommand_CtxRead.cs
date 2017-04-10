namespace Debugger.Server.Commands
{
    public class DebugCommand_CtxRead : IDebugCommand
    {
        public DoneCommandDelegate Done { get; set; }
        public byte[] CommandBuffer { get; } = new byte[1] {DebuggerCommandCodes.DEBUG_REQ_GET_CTX_ADDR};
        public uint ResponseSize { get; } = 4;
    }
}