using System.Collections.Generic;

namespace AVR.Debugger.Interfaces
{
    public class ValueGroup
    {
        public string Name { get; set; }
        public string Caption { get; set; }

        public List<ValueGroupValue> Values { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
