using System;

namespace ELFSharp.DWARF.Sections.Models
{
    public class DataBlock
    {
        public DataBlock(Func<byte> reader)
        {
            Size = reader();
            var dataCnt = 0;
            Data = new byte[Size];
            while (dataCnt < Size)
                Data[dataCnt++] = reader();
        }

        public byte Size { get; set; }
        public byte[] Data { get; set; }
    }
}