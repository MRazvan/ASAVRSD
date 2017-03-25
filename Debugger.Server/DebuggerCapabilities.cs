using System;

namespace Debugger.Server
{
    [Flags]
    public enum DebuggerCapabilities
    {
        CAPS_RAM_R_BIT				=(1 << 0),
        CAPS_RAM_W_BIT				=(1 << 1),
        CAPS_FLASH_R_BIT			=(1 << 2),
        CAPS_FLASH_W_BIT			=(1 << 3),
        CAPS_EEPROM_R_BIT			=(1 << 4),
        CAPS_EEPROM_W_BIT			=(1 << 5),
        CAPS_EXECUTE_BIT			=(1 << 6),
        CAPS_DBG_CTX_ADDR_BIT		=(1 << 7),
        CAPS_UART_HIGHSPEED_BIT		=(1 << 8),
        CAPS_SAVE_CONTEXT_BIT		=(1 << 9),
        CAPS_SINGLE_STEP_BIT        =(1 << 10),
    }
}
