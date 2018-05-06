namespace AVR.Debugger.Interfaces
{
    public class BitField
    {
        public string Caption { get; set; }
        public string Name { get; set; }
        public long Mask { get; set; }
        public ValueGroup Values { get; set; }

        public override string ToString()
        {
            return Caption ?? Name;
        }
    }
}