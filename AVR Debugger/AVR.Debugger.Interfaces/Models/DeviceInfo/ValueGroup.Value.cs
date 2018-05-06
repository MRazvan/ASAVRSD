namespace AVR.Debugger.Interfaces
{
    public class ValueGroupValue
    {
        public string Caption { get; set; }
        public long Value { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Caption;
        }
    }
}
