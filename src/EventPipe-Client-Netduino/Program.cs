namespace EventPipe.Client.Netduino
{
    using System;
    using System.IO.Ports;
    using System.Threading;
    using EventPipe.Client.Netduino.Drivers;
    using EventPipe.Client.Netduino.IO;
    using EventPipe.Common.Data;
    using MicroLiquidCrystal;
    using SecretLabs.NETMF.Hardware.Netduino;

    public class Program
    {
        private const int availableRegisters = 4;

        public Program()
        {
            using (var lcdProvider = new GpioLcdTransferProvider(Pins.GPIO_PIN_D7, Pins.GPIO_PIN_D8, Pins.GPIO_PIN_D9, Pins.GPIO_PIN_D10, Pins.GPIO_PIN_D11, Pins.GPIO_PIN_D12))
            using (var shiftRegisterDriver = new ShiftRegisterDriver(Pins.GPIO_PIN_D2, Pins.GPIO_PIN_D4, Pins.GPIO_PIN_D3, false, availableRegisters))
            using (var serialPort = new SerialPort(SerialPorts.COM1, 9600, Parity.None, 8, StopBits.One))
            using (var packetReader = new SerialPacketReader(serialPort))
            {
                var lcd = new Lcd(lcdProvider);
                lcd.Begin(20, 4);

                // turn on the lcd backlight
                using (var shiftRegisterSession = shiftRegisterDriver.AcquireSessionLock())
                {
                    shiftRegisterSession.Clear();
                    shiftRegisterSession.Write(1);
                }

                lcd.Write("EventPipe v1.0");
                lcd.SetCursorPosition(0, 1);

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
                        lcd.Write("Xbee link...    ");
                        serialPort.Open();
                        lcd.Write("[OK]");
                        lcd.SetCursorPosition(14, 3);
                        lcd.Write("Ready.");
                    }

                    var packet = packetReader.Read();
                    using (var shiftRegisterSession = shiftRegisterDriver.AcquireSessionLock())
                    {
                        ProcessPacket(packet, shiftRegisterSession, lcd);
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
            for (var i = 1; i < registers; i++)
            {
                shiftRegisterSession.Write((byte)((1 << i) | 1)); // keep the backlight on
                Thread.Sleep(150);
            }

            shiftRegisterSession.Write(255);
            Thread.Sleep(300);

            shiftRegisterSession.Write(1); // keep the backlight on
        }

        private static void ProcessPacket(SerialPacket packet, ShiftRegisterDriver.Session shiftRegisterSession, Lcd lcd)
        {
            switch (packet.Payload[0])
            {
                case StatusCode.Away:
                    var user = packet.Payload.Substring(1, packet.Payload.Length - 1);

                    // first and third bit enabled
                    shiftRegisterSession.Write(5);
                    lcd.Clear();
                    lcd.SetCursorPosition(0, 1);
                    lcd.Write(user + " (away)");
                    break;
                case StatusCode.Busy:
                    user = packet.Payload.Substring(1, packet.Payload.Length - 1);

                    // first and fourth bit enabled
                    shiftRegisterSession.Write(9);
                    lcd.Clear();
                    lcd.SetCursorPosition(0, 1);
                    lcd.Write(user + " (busy)");
                    break;
                case StatusCode.Free:
                    user = packet.Payload.Substring(1, packet.Payload.Length - 1);

                    // first and second bit enabled
                    shiftRegisterSession.Write(3);
                    lcd.Clear();
                    lcd.SetCursorPosition(0, 1);
                    lcd.Write(user + " (free)");
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
                    shiftRegisterSession.Write(1); // turn backlight on
                    LcdHelper.Write20X4(lcd, packet.Payload);
                    break;
            }
        }
    }

    public static class LcdHelper
    {
        private const string RowPadding = "                    ";  // 20 chars

        /// <summary>
        /// 20x4 LCDs wrap rows in order 0-&gt;2-&gt;1-&gt;3 due to its memory configuration. This compensates by modifiying the string to write.
        /// </summary>
        /// <param name="lcd">
        /// The lcd module.
        /// </param>
        /// <param name="text">
        /// The text to write to the LCD.
        /// </param>
        public static void Write20X4(Lcd lcd, string text)
        {
            lcd.Clear();
            lcd.Home();

            var memoryMappedText = text.Substring(0, Math.Min(text.Length, 20));
            if (text.Length > 40)
            {
                memoryMappedText += PadRight(text.Substring(40, Math.Min(text.Length - 40, 20)));
                memoryMappedText += PadRight(text.Substring(20, Math.Min(text.Length - 20, 20)));

                if (text.Length > 60)
                {
                    memoryMappedText += text.Substring(60, Math.Min(text.Length - 60, 20));
                }
            }
            else if (text.Length > 20)
            {
                memoryMappedText += RowPadding;
                memoryMappedText += PadRight(text.Substring(20, Math.Min(text.Length - 20, 20)));
            }

            lcd.Write(memoryMappedText);
        }

        private static string PadRight(string text)
        {
            return text + RowPadding.Substring(0, 20 - text.Length);
        }
    }
}
