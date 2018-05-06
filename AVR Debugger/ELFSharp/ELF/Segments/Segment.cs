using System;
using System.IO;
using MiscUtil.IO;

namespace ELFSharp.ELF.Segments
{
    public sealed class Segment<T> : ISegment
    {
        private readonly Class elfClass;

        private readonly long headerOffset;
        private readonly Func<EndianBinaryReader> readerSource;

        internal Segment(long headerOffset, Class elfClass, Func<EndianBinaryReader> readerSource)
        {
            this.readerSource = readerSource;
            this.headerOffset = headerOffset;
            this.elfClass = elfClass;
            ReadHeader();
        }

        public T Address { get; private set; }

        public T PhysicalAddress { get; private set; }

        public T Size { get; private set; }

        public T Alignment { get; private set; }

        public long FileSize { get; private set; }

        public long Offset { get; private set; }

        public SegmentType Type { get; private set; }

        public SegmentFlags Flags { get; private set; }

        /// <summary>
        ///     Gets array containing complete segment image, including
        ///     the zeroed section.
        /// </summary>
        /// <returns>
        ///     Segment image as array.
        /// </returns>
        public byte[] GetContents()
        {
            // TODO: large segments
            using (var reader = ObtainReader(Offset))
            {
                var result = new byte[Size.To<int>()];
                var fileImage = reader.ReadBytesOrThrow(checked((int) FileSize));
                fileImage.CopyTo(result, 0);
                return result;
            }
        }

        public byte[] GetRawHeader()
        {
            using (var reader = ObtainReader(headerOffset))
            {
                return reader.ReadBytesOrThrow(elfClass == Class.Bit32 ? 32 : 56);
            }
        }

        public override string ToString()
        {
            return string.Format("{2}: size {3}, @ 0x{0:X}", Address, PhysicalAddress, Type, Size);
        }

        private void ReadHeader()
        {
            using (var reader = ObtainReader(headerOffset))
            {
                Type = (SegmentType) reader.ReadUInt32();
                if (elfClass == Class.Bit64)
                    Flags = (SegmentFlags) reader.ReadUInt32();
                // TODO: some functions?s
                Offset = elfClass == Class.Bit32 ? reader.ReadUInt32() : reader.ReadInt64();
                Address = (elfClass == Class.Bit32 ? reader.ReadUInt32() : reader.ReadUInt64()).To<T>();
                PhysicalAddress = (elfClass == Class.Bit32 ? reader.ReadUInt32() : reader.ReadUInt64()).To<T>();
                FileSize = elfClass == Class.Bit32 ? reader.ReadInt32() : reader.ReadInt64();
                Size = (elfClass == Class.Bit32 ? reader.ReadUInt32() : reader.ReadUInt64()).To<T>();
                if (elfClass == Class.Bit32)
                    Flags = (SegmentFlags) reader.ReadUInt32();
                Alignment = (elfClass == Class.Bit32 ? reader.ReadUInt32() : reader.ReadUInt64()).To<T>();
            }
        }

        private EndianBinaryReader ObtainReader(long givenOffset)
        {
            var reader = readerSource();
            reader.BaseStream.Seek(givenOffset, SeekOrigin.Begin);
            return reader;
        }
    }
}