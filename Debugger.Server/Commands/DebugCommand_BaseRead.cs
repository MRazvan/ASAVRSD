namespace Debugger.Server.Commands
{
    public class DebugCommand_BaseRead : IDebugCommand
    {
        public byte[] CommandBuffer { get; private set; }
            = new byte[5] { 0x00, 0x00, 0x00, 0x00, 0x00 };

        public uint ResponseSize { get; private set; }
        public uint RequestSize { get; set; }
        public uint Address { get; set; }

        public DebugCommand_BaseRead(uint addr, uint size, byte command)
        {
            Address = addr;
            RequestSize = size;
            ResponseSize = size;

            CommandBuffer[0] = command;
            CommandBuffer[1] = (byte)(addr >> 8);
            CommandBuffer[2] = (byte)(addr & 0xFF);
            CommandBuffer[3] = (byte)(size >> 8);
            CommandBuffer[4] = (byte)(size & 0xFF);
        }
    }
}
