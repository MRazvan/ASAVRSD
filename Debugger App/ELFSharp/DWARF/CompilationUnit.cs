using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ELFSharp.DWARF.Enums;
using ELFSharp.DWARF.Sections.Models;
using MiscUtil.IO;

namespace ELFSharp.DWARF
{
    public class CompilationUnit
    {
        private readonly DWARFData _data;
        private readonly EndianBinaryReader _reader;

        internal CompilationUnit(DWARFData data, EndianBinaryReader reader, long offset)
        {
            _reader = reader;
            Offset = offset;
            _data = data;
            ParseCompileUnit();
            ParseDie();
        }

        public long Offset { get; }
        public uint Length { get; private set; }
        public ushort Version { get; private set; }
        public uint AbbrevOffset { get; private set; }
        public byte AddressSize { get; private set; }
        public long DieOffset { get; private set; }
        public AbbreviationTable AbbrevTable { get; private set; }

        public List<DebugInfoEntry> DIEList { get; } = new List<DebugInfoEntry>();

        private void ParseDie()
        {
            var boundry = Offset + Length + 4;
            var die_offset = DieOffset;
            while (die_offset < boundry)
            {
                var die = new DebugInfoEntry(_data, _reader, this, die_offset);
                die_offset += die.Size;
                DIEList.Add(die);
            }

            FixDieRef();

            UnflatenTree();
        }

        private void UnflatenTree()
        {
            var _parents = new Stack<DebugInfoEntry>();
            _parents.Push(DIEList.First());
            for (var i = 1; i < DIEList.Count; i++)
            {
                var die = DIEList[i];
                if (!die.IsNull())
                {
                    var parent = _parents.Peek();
                    die.Parent = parent;
                    parent.Children.Add(die);
                    if (die.HasChildren)
                        _parents.Push(die);
                }
                else
                {
                    if (_parents.Count > 0)
                        _parents.Pop();
                }
            }
        }

        private void FixDieRef()
        {
            foreach (var die in DIEList.Where(d => d.Attributes != null))
            {
                var refAttributes = die.Attributes.Where(
                        a =>
                            a.Form == EForm.DW_FORM_ref1 || a.Form == EForm.DW_FORM_ref2 || a.Form == EForm.DW_FORM_ref4 ||
                            a.Form == EForm.DW_FORM_ref8)
                    .ToList();
                foreach (var refAttribute in refAttributes)
                {
                    var offset = Convert.ToInt64(refAttribute.Raw_value) + Offset;
                    refAttribute.Value = DIEList.FirstOrDefault(d => d.Offset == offset);
                }
            }
        }

        private void ParseCompileUnit()
        {
            _reader.Seek((int) Offset, SeekOrigin.Begin);
            Length = _reader.ReadUInt32();
            Version = _reader.ReadUInt16();
            AbbrevOffset = _reader.ReadUInt32();
            AddressSize = _reader.ReadByte();
            DieOffset = _reader.BaseStream.Position;
            AbbrevTable = _data.AbbrevSection.GetAbbreviationTable(AbbrevOffset);
        }
    }
}