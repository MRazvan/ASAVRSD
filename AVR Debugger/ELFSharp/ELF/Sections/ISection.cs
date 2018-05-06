using System;
using MiscUtil.IO;

namespace ELFSharp.ELF.Sections
{
    public interface ISection
    {
        SectionHeader Header { get; }
        string Name { get; }
        uint NameIndex { get; }
        SectionType Type { get; }
        SectionFlags Flags { get; }
        EndianBinaryReader GetSectionStream();
        Func<EndianBinaryReader> Reader();
        byte[] GetContents();
    }
}