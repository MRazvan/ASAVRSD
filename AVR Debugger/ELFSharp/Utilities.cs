using System;
using System.IO;
using System.Text;

namespace ELFSharp
{
    public static class Utilities
    {
        public static byte[] ReadBytesOrThrow(this Stream stream, int count)
        {
            var result = new byte[count];
            while (count > 0)
                count -= stream.Read(result, result.Length - count, count);
            return result;
        }

        public static long ReadSLEB128(this Stream stream, int offset = -1)
        {
            long value = 0;
            var shift = 0;

            byte bt = 0;

            if (offset != -1)
                stream.Seek(offset, SeekOrigin.Begin);
            do
            {
                var data = stream.ReadByte();
                if (data == -1)
                    break;
                bt = (byte) data;

                value |= (long) (bt & 0x7f) << shift;
                shift += 7;
            } while (bt >= 0x80);

            // Sign extend negative numbers.
            if ((bt & 0x40) != 0)
                value |= -1L << shift;

            return value;
        }

        public static ulong ReadULEB128(this Stream stream, int offset = -1)
        {
            ulong value = 0;
            var shift = 0;
            if (offset != -1)
                stream.Seek(offset, SeekOrigin.Begin);
            while (true)
            {
                var data = stream.ReadByte();
                if (data == -1)
                    break;
                var bt = (byte) data;
                value += (ulong) (bt & 0x7f) << shift;
                if (bt < 0x80) break;
                shift += 7;
            }
            return value;
        }

        public static string ReadCStr(this Stream stream, int offset = -1)
        {
            var sb = new StringBuilder();
            if (offset != -1)
                stream.Seek(offset, SeekOrigin.Begin);
            while (true)
            {
                var data = stream.ReadByte();
                if (data == -1)
                    break;
                if (data == 0)
                    break;
                sb.Append(Convert.ToChar((byte) data));
            }
            return sb.ToString();
        }
    }
}