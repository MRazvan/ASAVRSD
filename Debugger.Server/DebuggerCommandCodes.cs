namespace Debugger.Server
{
    public static class DebuggerCommandCodes
    {
        public const byte DEBUG_REQ_CONTINUE			= 0x02;
        public const byte DEBUG_REQ_GET_CTX_ADDR		= 0x03;
        public const byte DEBUG_REQ_UART_HIGH_SPEED	    = 0x05;
        public const byte DEBUG_REQ_READ_RAM			= 0x10;
        public const byte DEBUG_REQ_WRITE_RAM			= 0x11;
        public const byte DEBUG_REQ_READ_FLASH		    = 0x12;
        public const byte DEBUG_REQ_WRITE_FLASH		    = 0x13;
        public const byte DEBUG_REQ_READ_EEPROM		    = 0x14;
        public const byte DEBUG_REQ_WRITE_EEPROM		= 0x15;
        public const byte DEBUG_REQ_SINGLE_STEP         = 0x16;
    }
}
