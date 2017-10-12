using System.Collections.Generic;
using System.IO;
using System.Linq;
using ELFSharp.ELF.Sections;
using MiscUtil.IO;

namespace ELFSharp.DWARF.Sections
{
    public class DebugStringsSection
    {
        private readonly SectionHeader _sectionHeader;
        private readonly EndianBinaryReader _stream;
        private readonly Dictionary<long, string> _strings = new Dictionary<long, string>();

        internal DebugStringsSection(ISection section)
        {
            _stream = section.GetSectionStream();
            _sectionHeader = section.Header;
            ParseStrings();
        }

        public List<string> GetStrings()
        {
            return _strings.Values.ToList();
        }

        public string GetString(long offset)
        {
            if (_strings.ContainsKey(offset))
                return _strings[offset];
            var parsedString = InternalGetString(offset);
            _strings[offset] = parsedString;
            return parsedString;
        }

        private string InternalGetString(long offset)
        {
            _stream.Seek((int) offset, SeekOrigin.Begin);
            return _stream.BaseStream.ReadCStr();
        }

        private void ParseStrings()
        {
            _stream.Seek(0, SeekOrigin.Begin);
            long strOffset = 0;
            while (strOffset < _sectionHeader.Size)
            {
                var str = _stream.BaseStream.ReadCStr();
                _strings[strOffset] = str;
                strOffset = _stream.BaseStream.Position;
            }
        }
    }
}