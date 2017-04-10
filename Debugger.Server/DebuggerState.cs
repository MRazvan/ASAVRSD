namespace Debugger.Server
{
    internal enum DebuggerState
    {
        NotConnected,
        WaitResponse,
        Connected,
        Stopping
    }
}