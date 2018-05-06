using System.Collections.Generic;

namespace AVR.Debugger.Interfaces
{
    public class Device
    {
        public List<Variant> Variants { get; set; }
        public string Name { get; set; }
        public string Architecture { get; set; }
        public string Family { get; set; }
        public List<ValueGroup> ValueGroups { get; set; }
        public List<AddressSpace> AddressSpace { get; set; }
        public List<Module> Modules { get; set; }
        public List<Interrupt> Interrupts { get; set; }
        public long Signature { get; set; }
    }
}