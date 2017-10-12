using System.Collections.Generic;
using System.Linq;
using ELFSharp.ELF.Sections;
using MiscUtil.IO;

namespace ELFSharp.DWARF.Sections
{
    public class DebugAbbrevSection
    {
        private readonly Dictionary<ulong, AbbreviationTable> _abbreviationTables =
            new Dictionary<ulong, AbbreviationTable>();

        private readonly EndianBinaryReader _stream;

        internal DebugAbbrevSection(ISection section)
        {
            _stream = section.GetSectionStream();
        }

        public List<AbbreviationTable> GetAbbreviationTables()
        {
            return _abbreviationTables.Values.ToList();
        }

        public AbbreviationTable GetAbbreviationTable(ulong offset)
        {
            if (!_abbreviationTables.ContainsKey(offset))
                _abbreviationTables[offset] = new AbbreviationTable(_stream, offset);
            return _abbreviationTables[offset];
        }
    }
}