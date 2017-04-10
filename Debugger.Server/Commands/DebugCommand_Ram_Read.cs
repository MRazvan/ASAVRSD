namespace Debugger.Server.Commands
{
    public class DebugCommand_Ram_Read : DebugCommand_BaseRead
    {
        public DebugCommand_Ram_Read(uint addr, uint size) :
            base(addr, size, DebuggerCommandCodes.DEBUG_REQ_READ_RAM)
        {
        }
    }
}