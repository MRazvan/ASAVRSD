using System.Collections.Generic;

namespace ELFSharp.DWARF.Sections
{
    public partial class DebugLineSection
    {
        internal partial class LineProgramHeader
        {
            public uint Length { get; set; }
            public ushort Version { get; set; }
            public uint HeaderLength { get; set; }
            public byte MinInstructionLength { get; set; }
            public byte MaxOperationsPerInstruction { get; set; }
            public byte DefaultIsStatement { get; set; }
            public sbyte LineBase { get; set; }
            public byte LineRange { get; set; }
            public byte OpCodeBase { get; set; }
            public byte[] OpCodeLengths { get; set; }
            public List<string> IncludeDirectories { get; set; }
            public List<FileEntry> IncludeFiles { get; set; }
        }
    }
}