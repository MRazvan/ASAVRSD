using System;
using System.IO.Ports;

namespace Debugger.Server.Transports
{
    public class SerialTransport : ITransport
    {
        private readonly byte[] _byteBuffer = new byte[1];
        private string _port;
        private SerialPort _serial;
        private int _speed;

        public void Connect()
        {
            if (_serial == null)
            {
                _serial = new SerialPort(_port, _speed);
                _serial.Open();
            }
        }

        public void Disconnect()
        {
            _serial.Close();
            _serial = null;
        }

        public byte ReadByte()
        {
            return (byte) (_serial.ReadByte() & 0xFF);
        }

        public void SetPort(string port)
        {
            _port = port;
        }

        public void SetSpeed(int speed)
        {
            _speed = speed;
        }

        public void Write(byte[] buffer)
        {
            if (_serial == null) throw new InvalidOperationException();
            _serial.Write(buffer, 0, buffer.Length);
        }

        public void WriteByte(byte data)
        {
            _byteBuffer[0] = data;
            Write(_byteBuffer);
        }

        public void ResetTarget()
        {
            _serial.DtrEnable = true;
        }
    }
}