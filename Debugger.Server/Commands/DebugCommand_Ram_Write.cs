namespace Debugger.Server.Commands
{
    public class DebugCommand_Ram_Write : DebugCommand_BaseWrite
    {
        public DebugCommand_Ram_Write(uint address, byte[] data)
            : base(address, data, DebuggerCommandCodes.DEBUG_REQ_WRITE_RAM)
        {
            // We don't wait for response in general
            ResponseSize = 0;
        }
    }
}