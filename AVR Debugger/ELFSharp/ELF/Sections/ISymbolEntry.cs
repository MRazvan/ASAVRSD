namespace ELFSharp.ELF.Sections
{
    public interface ISymbolEntry
    {
        string Name { get; }
        long Value { get; }

        long Size { get; }
        SymbolBinding Binding { get; }
        SymbolType Type { get; }
        bool IsPointedIndexSpecial { get; }
        ISection PointedSection { get; }
        ushort PointedSectionIndex { get; }
    }
}