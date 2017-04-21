﻿namespace Debugger.Server.Commands
{
    public class DebugCommand_EEPROM_Read : DebugCommand_BaseRead
    {
        public DebugCommand_EEPROM_Read(uint addr, uint size)
            : base(addr, size, DebuggerCommandCodes.DEBUG_REQ_READ_EEPROM)
        {
        }
    }
}