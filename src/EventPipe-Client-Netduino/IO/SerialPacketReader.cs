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
            var payload = string.Empty;
            var buff = new byte[1];
            while (true)
            {
                if (this.serialPort.Read(buff, 0, buff.Length) > 0)
                {
                    var buffChars = Encoding.UTF8.GetChars(buff);
                    if (buffChars[0] == '\n')
                    {
                        break;
                    }

                    payload += buffChars[0];    
                }
                else
                {
                    break;
                }                
            }

            return new SerialPacket(payload);
        }

        public void Dispose()
        {
        }
    }
}
