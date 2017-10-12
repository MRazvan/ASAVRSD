using ELFSharp.DWARF.Sections;
using ELFSharp.ELF;

namespace ELFSharp.DWARF
{
    public class DWARFData
    {
        public DWARFData(IELF elf)
        {
            // The order of loaded sections matters, do not change.
            StringSection = new DebugStringsSection(elf.GetSection(".debug_str"));
            AbbrevSection = new DebugAbbrevSection(elf.GetSection(".debug_abbrev"));
            InfoSection = new DebugInfoSection(this, elf.GetSection(".debug_info"));
            LineSection = new DebugLineSection(this, elf.GetSection(".debug_line"));
        }

        public DebugStringsSection StringSection { get; }
        public DebugAbbrevSection AbbrevSection { get; }
        public DebugInfoSection InfoSection { get; }
        public DebugLineSection LineSection { get; set; }
    }
}