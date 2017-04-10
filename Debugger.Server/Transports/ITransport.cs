namespace Debugger.Server.Transports
{
    public interface ITransport
    {
        void Connect();
        void Disconnect();
        void SetPort(string port);
        void SetSpeed(int speed);
        byte ReadByte();
        void WriteByte(byte data);
        void Write(byte[] buffer);
        void ResetTarget();
    }
}