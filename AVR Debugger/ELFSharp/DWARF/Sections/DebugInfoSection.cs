using System.Collections.Generic;
using ELFSharp.ELF.Sections;
using MiscUtil.IO;

namespace ELFSharp.DWARF.Sections
{
    public class DebugInfoSection
    {
        private readonly DWARFData _data;
        private readonly SectionHeader _sheader;
        private readonly EndianBinaryReader _stream;

        internal DebugInfoSection(DWARFData data, ISection section)
        {
            _stream = section.GetSectionStream();
            _sheader = section.Header;
            _data = data;
            ParseCompilationUnits();
        }

        public List<CompilationUnit> CompilationUnits { get; } = new List<CompilationUnit>();

        private void ParseCompilationUnits()
        {
            long offset = 0;
            while (offset < _sheader.Size)
            {
                var cu = new CompilationUnit(_data, _stream, offset);
                CompilationUnits.Add(cu);
                offset += cu.Length + 4;
            }
        }
    }
}