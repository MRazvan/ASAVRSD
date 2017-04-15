namespace AVR.Debugger
{
    public class CpuState
    {
        public uint PC { get; set; }
        public uint Stack { get; set; }
        public byte[] Registers { get; set; }
    }
}
