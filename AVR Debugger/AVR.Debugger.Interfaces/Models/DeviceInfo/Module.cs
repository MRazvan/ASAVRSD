using System.Collections.Generic;

namespace AVR.Debugger.Interfaces
{
    public class Module
    {
        public string Name { get; set; }
        public string Caption { get; set; }
        public long BaseOffset { get; set; }
        public AddressSpace AddressSpace { get; set; }
        public List<Register> Registers { get; set; }
        public override string ToString()
        {
            return $"{Caption} ({Name})";
        }
    }
}