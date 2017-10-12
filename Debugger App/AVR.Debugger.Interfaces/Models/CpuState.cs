namespace AVR.Debugger.Interfaces.Models
{
    public class CpuState
    {
        public uint PC { get; set; }
        public uint Stack { get; set; }
        public byte[] Registers { get; set; }
    }
}