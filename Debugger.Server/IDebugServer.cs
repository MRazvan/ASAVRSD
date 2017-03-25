using System.Threading.Tasks;

namespace Debugger.Server
{
    public interface IDebugServer
    {
        bool InDebug { get; }
        DebuggerCapabilities Caps { get; }
        byte DebugVersion { get; }
        uint DeviceSignature { get; }

        event DebuggerDetachedDelegate DebuggerDetached;
        event DebuggerAttachedDelegate DebuggerAttached;
        event DebuggerDisconnectedDelegate DebuggerDisconnected;
        event UnknowDataDelegate UnknownData;

        Task<byte[]> AddCommand(IDebugCommand cmd);
        void Start();
        void Stop();
        void Continue();
    }
}