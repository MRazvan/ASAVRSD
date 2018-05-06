namespace ELFSharp.DWARF
{
    public class LineInfo
    {
        public FileInfo File { get; set; }
        public long Line { get; set; }
        public long Column { get; set; }
    }
}