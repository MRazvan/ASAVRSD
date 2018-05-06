namespace AVR.Debugger.Interfaces
{
    public class MemorySegment
    {
        public long Start { get; set; }
        public long Size { get; set; }
        public long PageSize { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public bool Executable { get; set; }
        public bool OnlyReadable { get; set; }
        public bool External { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
}