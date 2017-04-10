using System;

namespace Debugger.Server.Commands
{
    public class DebugCommand_BaseWrite : IDebugCommand
    {
        public DebugCommand_BaseWrite(uint address, byte[] data, byte command)
        {
            var size = data.Length;
            CommandBuffer = new byte[5 + size];
            CommandBuffer[0] = command;
            CommandBuffer[1] = (byte) (address >> 8);
            CommandBuffer[2] = (byte) (address & 0xFF);
            CommandBuffer[3] = (byte) (size >> 8);
            CommandBuffer[4] = (byte) (size & 0xFF);
            Array.Copy(data, 0, CommandBuffer, 5, size);
        }

        public byte[] CommandBuffer { get; protected set; }

        public uint ResponseSize { get; protected set; }
            = 0;
    }
}