using System;

namespace ELFSharp.MachO
{
    public sealed class Section
    {
        private readonly long offsetInSegment;
        private readonly Segment segment;

        public Section(string name, long address, long size, long offsetInSegment, int alignExponent, Segment segment)
        {
            Name = name;
            Address = address;
            Size = size;
            this.offsetInSegment = offsetInSegment;
            AlignExponent = alignExponent;
            this.segment = segment;
        }

        public string Name { get; private set; }
        public long Address { get; private set; }
        public long Size { get; }
        public int AlignExponent { get; private set; }

        public byte[] GetData()
        {
            var result = new byte[Size];
            Array.Copy(segment.GetData(), offsetInSegment, result, 0, Size);
            return result;
        }
    }
}