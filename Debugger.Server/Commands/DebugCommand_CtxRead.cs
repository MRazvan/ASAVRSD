namespace Debugger.Server.Commands
{
    public class DebugCommand_CtxRead : IDebugCommand
    {
        public byte[] CommandBuffer { get; } = new byte[1] {DebuggerCommandCodes.DEBUG_REQ_GET_CTX_ADDR};
        public uint ResponseSize { get; } = 4;
        public DoneCommandDelegate Done { get; set; }
    }
}
