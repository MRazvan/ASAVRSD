namespace AVR.Debugger
{
    public class Symbol
    {
        public string Name { get; set; }
        public string File { get; set; }
        public SymbolSection Type { get; set; }
        public uint Location { get; set; }
        public uint Size { get; set; }
    }
}
