namespace AVR.Debugger.Interfaces
{
    public class Interrupt
    {
        public long Index { get; set; }
        public string Name { get; set; }
        public string Caption { get; set; }

        public override string ToString()
        {
            return Caption;
        }
    }
}