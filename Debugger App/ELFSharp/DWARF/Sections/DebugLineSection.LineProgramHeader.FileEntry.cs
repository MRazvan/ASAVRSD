namespace ELFSharp.DWARF.Sections
{
    public partial class DebugLineSection
    {
        internal partial class LineProgramHeader
        {
            internal class FileEntry
            {
                public string Name { get; set; }
                public ulong DirIndex { get; set; }
                public ulong TimeOfChange { get; set; }
                public ulong Size { get; set; }
            }
        }
    }
}