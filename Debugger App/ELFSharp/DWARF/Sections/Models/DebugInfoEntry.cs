using System;
using System.Collections.Generic;
using System.IO;
using ELFSharp.DWARF.Enums;
using MiscUtil.IO;

namespace ELFSharp.DWARF.Sections.Models
{
    public class DebugInfoEntry
    {
        private static readonly Dictionary<EForm, Func<EndianBinaryReader, object>> sFormReaders;
        private readonly DWARFData _data;
        private readonly EndianBinaryReader _stream;

        static DebugInfoEntry()
        {
            sFormReaders = new Dictionary<EForm, Func<EndianBinaryReader, object>>
            {
                {EForm.DW_FORM_addr, reader => reader.ReadUInt32()},
                {EForm.DW_FORM_block1, reader => new DataBlock(reader.ReadByte)},
                {EForm.DW_FORM_block2, reader => new DataBlock(reader.ReadByte)},
                {EForm.DW_FORM_block4, reader => new DataBlock(reader.ReadByte)},
                {EForm.DW_FORM_block, reader => new DataBlock(reader.ReadByte)},
                {EForm.DW_FORM_data1, reader => reader.ReadByte()},
                {EForm.DW_FORM_data2, reader => reader.ReadUInt16()},
                {EForm.DW_FORM_data4, reader => reader.ReadUInt32()},
                {EForm.DW_FORM_data8, reader => reader.ReadUInt64()},
                {EForm.DW_FORM_sdata, reader => reader.BaseStream.ReadSLEB128()},
                {EForm.DW_FORM_udata, reader => reader.BaseStream.ReadULEB128()},
                {EForm.DW_FORM_string, reader => reader.BaseStream.ReadCStr()},
                {EForm.DW_FORM_strp, reader => reader.ReadUInt32()},
                {EForm.DW_FORM_flag, reader => reader.ReadByte()},
                {EForm.DW_FORM_ref1, reader => reader.ReadByte()},
                {EForm.DW_FORM_ref2, reader => reader.ReadUInt16()},
                {EForm.DW_FORM_ref4, reader => reader.ReadUInt32()},
                {EForm.DW_FORM_ref8, reader => reader.ReadUInt64()},
                {EForm.DW_FORM_ref_udata, reader => reader.BaseStream.ReadULEB128()},
                {EForm.DW_FORM_ref_addr, reader => reader.ReadUInt32()},
                {EForm.DW_FORM_indirect, reader => reader.BaseStream.ReadULEB128()},
                {EForm.DW_FORM_flag_present, reader => 1},
                {EForm.DW_FORM_sec_offset, reader => reader.ReadUInt32()},
                {EForm.DW_FORM_exprloc, reader => new DataBlock(reader.ReadByte)},
                {EForm.DW_FORM_ref_sig8, reader => reader.ReadUInt32()},
                {EForm.DW_FORM_GNU_strp_alt, reader => reader.ReadUInt32()},
                {EForm.DW_FORM_GNU_ref_alt, reader => reader.ReadUInt32()}
            };
        }

        public DebugInfoEntry(DWARFData data, EndianBinaryReader stream, CompilationUnit compilationUnit, long offset)
        {
            _data = data;
            _stream = stream;
            Unit = compilationUnit;
            Offset = offset;
            Parse();
        }

        public CompilationUnit Unit { get; }
        public long Offset { get; }
        public bool HasChildren { get; private set; }
        public ETag Tag { get; private set; }
        public Abbreviation Abbreviation { get; private set; }
        public List<DieAttribute> Attributes { get; private set; }
        public int Size { get; private set; }
        public DebugInfoEntry Parent { get; set; }
        public List<DebugInfoEntry> Children { get; private set; } = new List<DebugInfoEntry>();

        private void Parse()
        {
            _stream.Seek((int) Offset, SeekOrigin.Begin);
            var code = _stream.BaseStream.ReadULEB128();
            Size = 0;
            if (code == 0)
            {
                Size = (int) (_stream.BaseStream.Position - Offset);
                return;
            }
            Abbreviation = Unit.AbbrevTable.GetAbbreviation(code);
            Tag = Abbreviation.Tag;
            HasChildren = Abbreviation.HasChildren();

            Attributes = new List<DieAttribute>();
            foreach (var attribute in Abbreviation.Attributes)
            {
                var offset = _stream.BaseStream.Position;
                var data = ParseForm(attribute.Form);
                var convertedData = TranslateData(attribute.Form, data);
                Attributes.Add(new DieAttribute
                {
                    Name = attribute.Name,
                    Form = attribute.Form,
                    Value = convertedData,
                    Raw_value = data,
                    Offset = offset
                });
            }
            Size = (int) (_stream.BaseStream.Position - Offset);
        }

        private object TranslateData(EForm attributeForm, object data)
        {
            switch (attributeForm)
            {
                case EForm.DW_FORM_strp:
                    return _data.StringSection.GetString(Convert.ToInt64(data));
                case EForm.DW_FORM_flag:
                    return Convert.ToInt64(data) != 0;
                case EForm.DW_FORM_indirect:
                    var form = (EForm) Enum.Parse(typeof(EForm), (string) data);
                    data = ParseForm(form);
                    return TranslateData(form, data);
            }
            return data;
        }

        private object ParseForm(EForm form)
        {
            return sFormReaders.ContainsKey(form) ? sFormReaders[form](_stream) : null;
        }

        public bool IsNull()
        {
            return Tag == ETag.DW_TAG_null;
        }

        public override string ToString()
        {
            return $"{Abbreviation.Tag}";
        }
    }
}