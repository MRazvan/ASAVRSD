using System;
using System.Collections.Generic;
using System.IO;
using ELFSharp.DWARF.Enums;
using MiscUtil.IO;

namespace ELFSharp.DWARF.Sections
{
    public partial class DebugLineSection
    {
        internal partial class LineProgram
        {
            private readonly long _offset;
            private readonly EndianBinaryReader _stream;

            public LineProgram(EndianBinaryReader stream, long offset)
            {
                _stream = stream;
                _offset = offset;
                Header = new LineProgramHeader();
                ParseHeader();
                StartProgramOffset = _stream.BaseStream.Position;
                EndProgramOffset = offset + Header.Length + 4;
                ParseLineProgram();
            }

            internal LineProgramHeader Header { get; set; }
            public long EndProgramOffset { get; set; }
            public long StartProgramOffset { get; set; }
            internal List<State> StateEntries { get; set; }

            public LineInfo GetLine(long address)
            {
                State prevState = null;
                LineInfo line = null;

                foreach (var entry in StateEntries)
                {
                    if (entry == null)
                        continue;
                    if (prevState != null && prevState.Address <= address && address < entry.Address)
                    {
                        var file = Header.IncludeFiles[(int) (prevState.File - 1)];
                        line = new LineInfo
                        {
                            File = new FileInfo
                            {
                                Directory = Header.IncludeDirectories[(int) (file.DirIndex - 1)],
                                File = file.Name
                            },
                            Line = prevState.Line,
                            Column = prevState.Column
                        };
                        break;
                    }
                    prevState = entry;
                }

                return line;
            }

            public void ParseLineProgram()
            {
                Func<State> newState = () => new State {IsStatement = Convert.ToBoolean(Header.DefaultIsStatement)};

                StateEntries = new List<State>();
                var state = newState();

                Action<object, object, bool> addNewEntryState = (cmd, args, is_extended) =>
                {
                    StateEntries.Add(state.Clone());
                    state.BasicBlock = false;
                    state.PrologueEnd = false;
                    state.EpilogueBegin = false;
                };

                Action<object, object, bool> addEntryOldState = (cmd, args, is_extended) => { StateEntries.Add(null); };

                var offset = StartProgramOffset;
                _stream.BaseStream.Seek(offset, SeekOrigin.Begin);
                while (offset < EndProgramOffset)
                {
                    var opcode = _stream.ReadByte();
                    if (opcode >= Header.OpCodeBase)
                    {
                        var maxop = Header.MaxOperationsPerInstruction;
                        var adjustedOpCode = opcode - Header.OpCodeBase;
                        var operationsInAdvance = adjustedOpCode / Header.LineRange;
                        var addressAddend = Header.MinInstructionLength *
                                            ((state.OpIndex + operationsInAdvance) / maxop);
                        state.Address += addressAddend;
                        state.OpIndex = (state.OpIndex + operationsInAdvance) % maxop;
                        var lineAddend = Header.LineBase + adjustedOpCode % Header.LineRange;
                        state.Line += lineAddend;
                        addNewEntryState(opcode, new[] {lineAddend, addressAddend, state.OpIndex}, false);
                    }
                    else if (opcode == 0)
                    {
                        var instr_len = _stream.BaseStream.ReadULEB128();
                        var ex_opcode = _stream.ReadByte();
                        if (ex_opcode == (int) ELineProgram.DW_LNE_end_sequence)
                        {
                            state.EndSequence = true;
                            addNewEntryState(ex_opcode, null, true);
                            state = newState();
                        }
                        else if (ex_opcode == (int) ELineProgram.DW_LNE_set_address)
                        {
                            var operand = _stream.ReadUInt32();
                            state.Address = operand;
                            addEntryOldState(ex_opcode, new[] {operand}, true);
                        }
                        else if (ex_opcode == (int) ELineProgram.DW_LNE_define_file)
                        {
                            var fe = ParseFileEntry(_stream);
                            Header.IncludeFiles.Add(fe);
                            addEntryOldState(ex_opcode, new[] {fe}, true);
                        }
                        else
                        {
                            _stream.Seek((int) (instr_len - 1), SeekOrigin.Current);
                        }
                    }
                    else
                    {
                        if (opcode == (int) ELineProgram.DW_LNS_copy)
                        {
                            addNewEntryState(opcode, null, false);
                        }
                        else if (opcode == (int) ELineProgram.DW_LNS_advance_pc)
                        {
                            var operand = _stream.BaseStream.ReadULEB128();
                            var address_addend = operand * Header.MinInstructionLength;
                            state.Address += (long) address_addend;
                            addEntryOldState(opcode, new[] {address_addend}, false);
                        }
                        else if (opcode == (int) ELineProgram.DW_LNS_advance_line)
                        {
                            var operand = _stream.BaseStream.ReadSLEB128();
                            state.Line += (long) operand;
                        }
                        else if (opcode == (int) ELineProgram.DW_LNS_set_file)
                        {
                            var operand = _stream.BaseStream.ReadULEB128();
                            state.File = (long) operand;
                            addEntryOldState(opcode, new[] {operand}, false);
                        }
                        else if (opcode == (int) ELineProgram.DW_LNS_set_column)
                        {
                            var operand = _stream.BaseStream.ReadULEB128();
                            state.Column += (long) operand;
                            addEntryOldState(opcode, new[] {operand}, false);
                        }
                        else if (opcode == (int) ELineProgram.DW_LNS_negate_stmt)
                        {
                            state.IsStatement = !state.IsStatement;
                            addEntryOldState(opcode, null, false);
                        }
                        else if (opcode == (int) ELineProgram.DW_LNS_set_basic_block)
                        {
                            state.BasicBlock = true;
                            addEntryOldState(opcode, null, false);
                        }
                        else if (opcode == (int) ELineProgram.DW_LNS_const_add_pc)
                        {
                            var adjustedOpCode = 255 - Header.OpCodeBase;
                            var adjustedAddend = adjustedOpCode / Header.LineRange * Header.MinInstructionLength;
                            state.Address += adjustedAddend;
                            addEntryOldState(opcode, new[] {adjustedAddend}, false);
                        }
                        else if (opcode == (int) ELineProgram.DW_LNS_fixed_advance_pc)
                        {
                            var operand = _stream.ReadUInt16();
                            state.Address += operand;
                            addEntryOldState(opcode, new[] {operand}, false);
                        }
                        else if (opcode == (int) ELineProgram.DW_LNS_set_prologue_end)
                        {
                            state.PrologueEnd = true;
                            addEntryOldState(opcode, null, false);
                        }
                        else if (opcode == (int) ELineProgram.DW_LNS_set_epilogue_begin)
                        {
                            state.EpilogueBegin = true;
                            addEntryOldState(opcode, null, false);
                        }
                        else if (opcode == (int) ELineProgram.DW_LNS_set_isa)
                        {
                            var operand = _stream.BaseStream.ReadULEB128();
                            state.ISA = (long) operand;
                            addEntryOldState(opcode, new[] {operand}, false);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Unknown opcode {opcode}");
                        }
                    }
                    offset = _stream.BaseStream.Position;
                }
            }

            public void ParseHeader()
            {
                _stream.BaseStream.Seek(_offset, SeekOrigin.Begin);
                Header.Length = _stream.ReadUInt32();
                Header.Version = _stream.ReadUInt16();
                Header.HeaderLength = _stream.ReadUInt32();
                Header.MinInstructionLength = _stream.ReadByte();
                Header.MaxOperationsPerInstruction = 1;
                if (Header.Version >= 4)
                    Header.MaxOperationsPerInstruction = _stream.ReadByte();
                Header.DefaultIsStatement = _stream.ReadByte();
                Header.LineBase = _stream.ReadSByte();
                Header.LineRange = _stream.ReadByte();
                Header.OpCodeBase = _stream.ReadByte();
                Header.OpCodeLengths = _stream.ReadBytes(Header.OpCodeBase - 1);
                Header.IncludeDirectories = ReadIncludePaths(_stream);
                Header.IncludeFiles = ReadIncludeFiles(_stream);
                StartProgramOffset = _stream.BaseStream.Position;
            }

            private LineProgramHeader.FileEntry ParseFileEntry(EndianBinaryReader stream)
            {
                return new LineProgramHeader.FileEntry
                {
                    Name = stream.BaseStream.ReadCStr(),
                    DirIndex = stream.BaseStream.ReadULEB128(),
                    TimeOfChange = stream.BaseStream.ReadULEB128(),
                    Size = stream.BaseStream.ReadULEB128()
                };
            }

            private List<LineProgramHeader.FileEntry> ReadIncludeFiles(EndianBinaryReader stream)
            {
                var fileEntries = new List<LineProgramHeader.FileEntry>();
                while (true)
                {
                    var position = stream.BaseStream.Position;
                    var data = stream.ReadByte();
                    if (data == 0)
                        break;
                    stream.BaseStream.Seek(position, SeekOrigin.Begin);
                    fileEntries.Add(ParseFileEntry(stream));
                }
                return fileEntries;
            }

            private List<string> ReadIncludePaths(EndianBinaryReader stream)
            {
                var includeDirs = new List<string>();
                while (true)
                {
                    var position = stream.BaseStream.Position;
                    var data = stream.ReadByte();
                    if (data == 0)
                        break;
                    stream.BaseStream.Seek(position, SeekOrigin.Begin);
                    includeDirs.Add(stream.BaseStream.ReadCStr());
                }
                return includeDirs;
            }
        }
    }
}