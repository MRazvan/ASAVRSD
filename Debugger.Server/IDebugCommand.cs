namespace Debugger.Server
{
    public delegate void DoneCommandDelegate(byte[] response);
    public interface IDebugCommand
    {
        byte[] CommandBuffer { get; }
        uint ResponseSize { get; }
    }
}
