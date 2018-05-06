using System.Collections.Generic;

namespace AVR.Debugger.Interfaces
{
    public class Register
    {
        public string Caption { get; set; }
        public string Name { get; set; }
        public long Offset { get; set; }
        public long Size { get; set; }
        public long InitVal { get; set; }
        public long Mask { get; set; }
        public List<BitField> BitFields { get; set; }

        public override string ToString()
        {
            return Caption;
        }
    }
}