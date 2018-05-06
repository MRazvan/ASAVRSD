using System;

namespace ELFSharp.ELF.Sections
{
    public class SymbolEntry<T> : ISymbolEntry where T : struct
    {
        private readonly ELF<T> elf;

        public SymbolEntry(string name, T value, T size, SymbolBinding binding, SymbolType type, ELF<T> elf,
            ushort sectionIdx)
        {
            Name = name;
            Value = value;
            Size = size;
            Binding = binding;
            Type = type;
            this.elf = elf;
            PointedSectionIndex = sectionIdx;
        }

        public T Value { get; }

        public T Size { get; }

        public Section<T> PointedSection
        {
            get { return IsPointedIndexSpecial ? null : elf.GetSection(PointedSectionIndex); }
        }

        public SpecialSectionIndex SpecialPointedSectionIndex
        {
            get
            {
                if (IsPointedIndexSpecial)
                    return (SpecialSectionIndex) PointedSectionIndex;
                throw new InvalidOperationException("Given pointed section index does not have special meaning.");
            }
        }

        public string Name { get; }

        public SymbolBinding Binding { get; }

        public SymbolType Type { get; }

        public bool IsPointedIndexSpecial
        {
            get { return Enum.IsDefined(typeof(SpecialSectionIndex), PointedSectionIndex); }
        }

        ISection ISymbolEntry.PointedSection
        {
            get { return PointedSection; }
        }

        public ushort PointedSectionIndex { get; }

        long ISymbolEntry.Value
        {
            get { return Convert.ToInt64(Value); }
        }

        long ISymbolEntry.Size
        {
            get { return Convert.ToInt64(Size); }
        }

        public override string ToString()
        {
            return string.Format("[{3} {4} {0}: 0x{1:X}, size: {2}, section idx: {5}]",
                Name, Value, Size, Binding, Type, (SpecialSectionIndex) PointedSectionIndex);
        }
    }
}