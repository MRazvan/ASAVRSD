using System.Collections.Generic;
using System.IO;
using ELFSharp.DWARF.Enums;
using ELFSharp.DWARF.Sections.Models;
using MiscUtil.IO;

namespace ELFSharp.DWARF
{
    public class AbbreviationTable
    {
        private readonly Dictionary<ulong, Abbreviation> _abbreviations;
        private readonly ulong _offset;
        private readonly EndianBinaryReader _readerSource;

        public AbbreviationTable(EndianBinaryReader readerSource, ulong offset)
        {
            _readerSource = readerSource;
            _offset = offset;
            _abbreviations = new Dictionary<ulong, Abbreviation>();
            ParseAbreviations();
        }

        public Abbreviation GetAbbreviation(ulong code)
        {
            // Leave it to throw in case of error
            return _abbreviations[code];
        }

        private void ParseAbreviations()
        {
            _readerSource.Seek((int) _offset, SeekOrigin.Begin);
            while (true)
            {
                var code = _readerSource.BaseStream.ReadULEB128();
                if (code == 0)
                    break;
                var abv = new Abbreviation
                {
                    Tag = (ETag) _readerSource.BaseStream.ReadULEB128(),
                    ChildrenFlag = (EChildren) _readerSource.ReadByte(),
                    Attributes = new List<AbbrevAttribute>()
                };
                while (true)
                {
                    var abat = new AbbrevAttribute
                    {
                        Name = (EAttributes) _readerSource.BaseStream.ReadULEB128(),
                        Form = (EForm) _readerSource.BaseStream.ReadULEB128()
                    };
                    abv.Attributes.Add(abat);
                    if (abat.Name == EAttributes.DW_AT_null && abat.Form == EForm.DW_FORM_null)
                        break;
                }
                _abbreviations[code] = abv;
            }
        }
    }
}