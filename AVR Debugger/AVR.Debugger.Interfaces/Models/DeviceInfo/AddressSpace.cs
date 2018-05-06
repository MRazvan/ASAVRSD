using System.Collections.Generic;

namespace AVR.Debugger.Interfaces
{
    public enum Endianess
    {
        None,
        Little,
        Big
    }

    public class AddressSpace
    {
        public long Start { get; set; }
        public long Size { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public Endianess Endianness { get; set; }
        public List<MemorySegment> MemorySegments { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
}