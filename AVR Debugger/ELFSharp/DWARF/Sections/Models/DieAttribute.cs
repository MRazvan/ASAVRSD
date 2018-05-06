using ELFSharp.DWARF.Enums;

namespace ELFSharp.DWARF.Sections.Models
{
    public class DieAttribute
    {
        public EAttributes Name { get; set; }
        public EForm Form { get; set; }
        public object Value { get; set; }
        public object Raw_value { get; set; }
        public long Offset { get; set; }

        public override string ToString()
        {
            return $"{Name} - {Form}   -   {Value}";
        }
    }
}