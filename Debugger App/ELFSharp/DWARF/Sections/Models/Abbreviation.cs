using System.Collections.Generic;
using ELFSharp.DWARF.Enums;

namespace ELFSharp.DWARF.Sections.Models
{
    public struct Abbreviation
    {
        public ETag Tag { get; set; }
        public EChildren ChildrenFlag { get; set; }
        public List<AbbrevAttribute> Attributes { get; set; }

        public bool HasChildren()
        {
            return ChildrenFlag == EChildren.DW_CHILDREN_yes;
        }

        public override string ToString()
        {
            return $"{Tag} - {ChildrenFlag}";
        }
    }
}