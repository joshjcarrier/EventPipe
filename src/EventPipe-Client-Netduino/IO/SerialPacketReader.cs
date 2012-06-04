namespace EventPipe.Client.Netduino.IO
{
    using System;
    using System.IO.Ports;
    using System.Text;
    using EventPipe.Common.Data;

    public class SerialPacketReader : IDisposable
    {
        private readonly SerialPort serialPort;

        public SerialPacketReader(SerialPort serialPort)
        {
            this.serialPort = serialPort;
        }

        public SerialPacket Read()
        {
            var buff = new byte[1];
            this.serialPort.Read(buff, 0, buff.Length);
            var chars = Encoding.UTF8.GetChars(buff);
            return new SerialPacket { Payload = new string(chars) };
        }

        public void Dispose()
        {
        }
    }
}
