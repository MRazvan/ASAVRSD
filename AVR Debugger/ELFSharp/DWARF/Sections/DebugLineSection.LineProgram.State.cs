namespace ELFSharp.DWARF.Sections
{
    public partial class DebugLineSection
    {
        internal partial class LineProgram
        {
            internal class State
            {
                public long Address;
                public bool BasicBlock;
                public long Column;
                public bool EndSequence;
                public bool EpilogueBegin;
                public long File;
                public long ISA;
                public bool IsStatement;
                public long Line;
                public int OpIndex;
                public bool PrologueEnd;

                public State Clone()
                {
                    return new State
                    {
                        BasicBlock = BasicBlock,
                        EndSequence = EndSequence,
                        PrologueEnd = PrologueEnd,
                        EpilogueBegin = EpilogueBegin,
                        Address = Address,
                        File = File,
                        Line = Line,
                        Column = Column,
                        OpIndex = OpIndex,
                        IsStatement = IsStatement,
                        ISA = ISA
                    };
                }
            }
        }
    }
}