namespace Debugger.Server.Commands
{
    public class DebugCommand_EEPROM_Write : DebugCommand_BaseWrite
    {
        public DebugCommand_EEPROM_Write(uint address, byte[] data)
            : base(address, data, DebuggerCommandCodes.DEBUG_REQ_WRITE_EEPROM)
        {
        }
    }
}