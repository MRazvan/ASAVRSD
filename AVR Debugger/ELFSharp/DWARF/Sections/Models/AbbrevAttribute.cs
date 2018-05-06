using ELFSharp.DWARF.Enums;

namespace ELFSharp.DWARF.Sections.Models
{
    public class AbbrevAttribute
    {
        public EAttributes Name { get; set; }
        public EForm Form { get; set; }

        public override string ToString()
        {
            return $"{Name} - {Form}";
        }
    }
}