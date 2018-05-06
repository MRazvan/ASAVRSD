using System;
using System.IO;
using MiscUtil.IO;

namespace ELFSharp.ELF.Sections
{
    public class Section<T> : ISection where T : struct
    {
        private readonly Func<EndianBinaryReader> readerSourceSourceSource;
        private EndianBinaryReader _sectionStream;

        internal Section(SectionHeader header, Func<EndianBinaryReader> readerSourceSourceSource)
        {
            Header = header;
            this.readerSourceSourceSource = readerSourceSourceSource;
        }

        public T RawFlags
        {
            get { return Header.RawFlags.To<T>(); }
        }

        public T LoadAddress
        {
            get { return Header.LoadAddress.To<T>(); }
        }

        public T Alignment
        {
            get { return Header.Alignment.To<T>(); }
        }

        public T EntrySize
        {
            get { return Header.EntrySize.To<T>(); }
        }

        public T Size
        {
            get { return Header.Size.To<T>(); }
        }

        public T Offset
        {
            get { return Header.Offset.To<T>(); }
        }

        public EndianBinaryReader GetSectionStream()
        {
            if (_sectionStream == null)
            {
                var reader = ObtainReader();
                var memStream = new MemoryStream(reader.ReadBytes((int) Header.Size));
                _sectionStream = new EndianBinaryReader(reader.BitConverter, new NonClosingStreamWrapper(memStream));
            }

            return _sectionStream;
        }

        public Func<EndianBinaryReader> Reader()
        {
            return ObtainReader;
        }

        public virtual byte[] GetContents()
        {
            using (var reader = ObtainReader())
            {
                return reader.ReadBytes(Convert.ToInt32(Header.Size));
            }
        }

        public string Name
        {
            get { return Header.Name; }
        }

        public uint NameIndex
        {
            get { return Header.NameIndex; }
        }

        public SectionType Type
        {
            get { return Header.Type; }
        }

        public SectionFlags Flags
        {
            get { return Header.Flags; }
        }

        public SectionHeader Header { get; }

        protected EndianBinaryReader ObtainReader()
        {
            var reader = readerSourceSourceSource();
            reader.BaseStream.Seek(Header.Offset, SeekOrigin.Begin);
            return reader;
        }

        public override string ToString()
        {
            return Header.ToString();
        }
    }
}