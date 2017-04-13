using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace DebugApp
{
    public class Registers
    {
        public const byte DDRB = 0x24;
        public const byte PINB = 0x23;

        public static readonly Dictionary<string, byte> RegisterMappings = new Dictionary<string, byte>
        {
            {"DDRB", DDRB},
            {"PINB", PINB}
        };

        public static uint? RegisterToAddress(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            if (RegisterMappings.ContainsKey(name.ToUpper()))
                return RegisterMappings[name.ToUpper()];
            return null;
        }
    }
}
