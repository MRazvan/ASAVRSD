﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ELFSharp.MachO
{
    public sealed class MachO
    {
        internal const int Architecture64 = 0x1000000;
        private readonly Command[] commands;
        private readonly string fileName;

        private readonly bool is64;

        internal MachO(string fileName, bool is64)
        {
            this.is64 = is64;
            this.fileName = fileName;
            using (var reader = new BinaryReader(OpenStream()))
            {
                reader.ReadBytes(4); // header, already checked
                Machine = (Machine) reader.ReadInt32();
                reader.ReadBytes(4); // we don't support the cpu subtype now
                FileType = (FileType) reader.ReadUInt32();
                var noOfCommands = reader.ReadInt32();
                var sizeOfCommands = reader.ReadInt32();
                reader.ReadBytes(4); // we don't support flags now
                if (is64)
                    reader.ReadBytes(4); // reserved
                commands = new Command[noOfCommands];
                ReadCommands(noOfCommands, sizeOfCommands, reader);
            }
        }

        public Machine Machine { get; private set; }

        public FileType FileType { get; private set; }

        public IEnumerable<T> GetCommandsOfType<T>()
        {
            return commands.Where(x => x != null).OfType<T>();
        }

        private void ReadCommands(int noOfCommands, int sizeOfCommands, BinaryReader reader)
        {
            for (var i = 0; i < noOfCommands; i++)
            {
                var loadCommandType = reader.ReadUInt32();
                var commandSize = reader.ReadUInt32();
                switch ((CommandType) loadCommandType)
                {
                    case CommandType.SymbolTable:
                        commands[i] = new SymbolTable(reader, OpenStream, is64);
                        break;
                    case CommandType.Main:
                        commands[i] = new EntryPoint(reader, OpenStream);
                        break;
                    case CommandType.Segment:
                    case CommandType.Segment64:
                        commands[i] = new Segment(reader, OpenStream, is64);
                        break;
                    default:
                        reader.ReadBytes((int) commandSize - 8); // 8 bytes is the size of the common command header
                        break;
                }
            }
        }

        private FileStream OpenStream()
        {
            return File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
    }
}