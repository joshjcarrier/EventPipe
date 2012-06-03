namespace EventPipe.Client.Netduino
{
    using System.IO.Ports;
    using System.Text;
    using System.Threading;
    using EventPipe.Common.Data;
    using Microsoft.SPOT.Hardware;
    using SecretLabs.NETMF.Hardware.Netduino;

    public class Program
    {
        public enum LyncStatus
        {
            Available = 3,
            Away = 4,
            Busy = 8
        }

        public static void Main()
        {
            var latchPin = new OutputPort(Pins.GPIO_PIN_D8, false);
            var clockPin = new OutputPort(Pins.GPIO_PIN_D12, false);
            var dataPin = new OutputPort(Pins.GPIO_PIN_D11, false);
            
            var sp = new SerialPort(SerialPorts.COM1, 9600, Parity.None, 8, StopBits.One);
            sp.Open();

            // example write
            //var buff = Encoding.UTF8.GetBytes("+++");
            //sp.Write(buff, 0, buff.Length);
            //sp.Close();

            // clear out last state
            latchPin.Write(false);
            ShiftOut((byte)0, clockPin, dataPin);
            latchPin.Write(true);

            var buff = new byte[1];
            while (true)
            {
                sp.Read(buff, 0, buff.Length);

                var chars = Encoding.UTF8.GetChars(buff);

                latchPin.Write(false);

                switch (chars[0])
                {
                    case StatusCode.Away:
                        ShiftOut(LyncStatus.Away, clockPin, dataPin);
                        break;
                    case StatusCode.Busy:
                        ShiftOut(LyncStatus.Busy, clockPin, dataPin);
                        break;
                    case StatusCode.Free:
                        ShiftOut(LyncStatus.Available, clockPin, dataPin);
                        break;
                }

                latchPin.Write(true);
            }
        }

        private static void ShiftOut(LyncStatus value, OutputPort clock, OutputPort dataPort)
        {
            ShiftOut((byte)value, clock, dataPort);
        }

        private static void ShiftOut(byte value, OutputPort clock, OutputPort dataPort)
        {
            // Lower Clock
            clock.Write(false);

            byte mask;
            for (int i = 0; i < 8; i++)
            {
                //if (_bitOrder == BitOrder.LSBFirst)
                //   mask = (byte)(1 << i);
                // else
                mask = (byte)(1 << (7 - i));

                dataPort.Write((value & mask) != 0);

                // Raise Clock to indicate write coming
                clock.Write(true);

                // Always write data
                dataPort.Write(true);

                // Lower Clock to indicate write complete
                clock.Write(false);
            }
        }

        #region demo code


        public static void DemoCounterUp(OutputPort latchPin, OutputPort clockPin, OutputPort dataPin)
        {
            // example counter up
            for (int x = 0; x < 40; x++)
            {
                // latch pin low so can write data
                latchPin.Write(false);

                ShiftOut((byte)(x % 16), clockPin, dataPin);
                for (int i = 0; i < 8; i++)
                {
                    dataPin.Write(x % (2 ^ i) == 0);
                    clockPin.Write(true);
                    clockPin.Write(false);
                }

                // commit changes
                latchPin.Write(true);
                Thread.Sleep(100);
            }
        }

        #endregion 
    }
}
