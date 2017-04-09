// PkgCmdID.cs
// MUST match PkgCmdID.h

namespace SoftwareDebuggerExtension
{
    internal static class PkgCmdIDList
    {
        public const uint cmdAttach = 0x100;
        public const uint cmdStep = 0x101;
        public const uint cmdContinue = 0x102;
        public const uint cmdSelectPort = 0x103;
        public const uint cmdSelectPortList = 0x104;

        public const uint cmdSelectBaud = 0x105;
        public const uint cmdSelectBaudList = 0x106;
        public const uint cmdOptions = 0x107;
    }
}