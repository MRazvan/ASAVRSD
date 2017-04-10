using System.Threading.Tasks;

namespace Debugger.Server
{
    internal class DebugCommandWrapper
    {
        public DebugCommandWrapper(IDebugCommand cmd)
        {
            Command = cmd;
            TCS = new TaskCompletionSource<byte[]>();
        }

        public IDebugCommand Command { get; }
        public TaskCompletionSource<byte[]> TCS { get; }
    }
}