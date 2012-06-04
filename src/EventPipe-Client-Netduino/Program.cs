namespace EventPipe.Client.Netduino
{
    using System.IO.Ports;
    using System.Threading;
    using EventPipe.Client.Netduino.Drivers;
    using EventPipe.Client.Netduino.IO;
    using EventPipe.Common.Data;
    using SecretLabs.NETMF.Hardware.Netduino;

    public class Program
    {
        private const int availableRegisters = 4;

        public Program()
        {
            using (var shiftRegisterDriver = new ShiftRegisterDriver(Pins.GPIO_PIN_D8, Pins.GPIO_PIN_D12, Pins.GPIO_PIN_D11, false, availableRegisters))
            using (var serialPort = new SerialPort(SerialPorts.COM1, 9600, Parity.None, 8, StopBits.One))
            using (var packetReader = new SerialPacketReader(serialPort))
            {
                // fancily clear out last shutdown state
                using (var shiftRegisterSession = shiftRegisterDriver.AcquireSessionLock())
                {
                    ClearFancy(availableRegisters, shiftRegisterSession);
                }

                while (true)
                {
                    // just in case the serial port closes, try again?
                    if (!serialPort.IsOpen)
                    {
                        serialPort.Open();
                    }

                    var packet = packetReader.Read();
                    using (var shiftRegisterSession = shiftRegisterDriver.AcquireSessionLock())
                    {
                        ProcessPacket(packet, shiftRegisterSession);
                    }
                }
            }
        }

        public static void Main()
        {
            new Program();
        }

        private static void ClearFancy(int registers, ShiftRegisterDriver.Session shiftRegisterSession)
        {
            for (var i = 0; i < registers; i++)
            {
                shiftRegisterSession.Write((byte)(1 << i));
                Thread.Sleep(150);
            }

            shiftRegisterSession.Write(255);
            Thread.Sleep(300);

            shiftRegisterSession.Clear();
        }

        private static void ProcessPacket(SerialPacket packet, ShiftRegisterDriver.Session shiftRegisterSession)
        {
            switch (packet.Payload[0])
            {
                case StatusCode.Away:
                    // third bit enabled
                    shiftRegisterSession.Write(4);
                    break;
                case StatusCode.Busy:
                    // fourth bit enabled
                    shiftRegisterSession.Write(8);
                    break;
                case StatusCode.Free:
                    // first two bits enabled
                    shiftRegisterSession.Write(3);
                    break;
                case 'q':
                    ClearFancy(8, shiftRegisterSession);
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    shiftRegisterSession.Write(byte.Parse(packet.Payload[0].ToString()));
                    break;
                default:
                    shiftRegisterSession.Clear();
                    break;
            }
        }
    }
}
