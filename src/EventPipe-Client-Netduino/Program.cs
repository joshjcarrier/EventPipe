namespace EventPipe.Client.Netduino
{
    using System;
    using System.IO.Ports;
    using System.Threading;
    using EventPipe.Client.Netduino.Devices;
    using EventPipe.Client.Netduino.Drivers;
    using EventPipe.Client.Netduino.IO;
    using EventPipe.Common.Data;
    using MicroLiquidCrystal;
    using SecretLabs.NETMF.Hardware.Netduino;

    public class Program
    {
        private readonly Lcd lcd;
        private readonly LcdScreen lcdScreen;
        private readonly AutoResetEvent lcdScreenRefreshReset;
        private readonly LyncCache lyncCache;

        public Program()
        {
            var lcdProvider = new GpioLcdTransferProvider(Pins.GPIO_PIN_D7, Pins.GPIO_PIN_D8, Pins.GPIO_PIN_D9, Pins.GPIO_PIN_D10, Pins.GPIO_PIN_D11, Pins.GPIO_PIN_D12);
            this.lcd = new Lcd(lcdProvider);
            this.lcdScreen = new LcdScreen(20, 4, this.lcd);
            this.lyncCache = new LyncCache();

            this.lcdScreenRefreshReset = new AutoResetEvent(false);

            var lcdScreenAutoRefreshThread = new Thread(this.RunLcdScreenAutoRefresh) { Priority = ThreadPriority.BelowNormal };
            lcdScreenAutoRefreshThread.Start();
        }

        public static void Main()
        {
            var program = new Program();
            program.Run();
        }

        public void Run()
        {
            try
            {
                this.RunCore();
            }
            catch (Exception ex)
            {
                this.lcd.Clear();
                this.lcd.Write(ex.InnerException != null ? ex.InnerException.Message : ex.Message + ex.StackTrace);
                throw;
            }
        }

        private void RunCore()
        {
            using (var shiftRegisterDriver = new ShiftRegisterDriver(Pins.GPIO_PIN_D2, Pins.GPIO_PIN_D4, Pins.GPIO_PIN_D3, false, 4))
            using (var serialPort = new SerialPort(SerialPorts.COM1, 9600, Parity.None, 8, StopBits.One))
            using (var packetReader = new SerialPacketReader(serialPort))
            {
                // turn on the lcd backlight
                using (var shiftRegisterSession = shiftRegisterDriver.AcquireSessionLock())
                {
                    shiftRegisterSession.Write(1);
                }

                var window = this.lcdScreen.CreatePage();
                window.MinimumWaitTime = 1000;
                window.Write(0, "EventPipe v1.0");
                this.lcdScreen.PushPage(window);
                this.lcdScreenRefreshReset.Set();
                
                while (true)
                {
                    // just in case the serial port closes, try again?
                    if (!serialPort.IsOpen)
                    {
                        serialPort.Open();
                    }

                    var packet = packetReader.Read();
                    this.ProcessPacket(packet, shiftRegisterDriver);
                }
            }
        }
        
        private void RunLcdScreenAutoRefresh()
        {
            while (true)
            {
                this.lcdScreenRefreshReset.WaitOne(1000, false);
                this.lcdScreen.WriteNextPage();
            }
        }

        private void ProcessPacket(SerialPacket packet, ShiftRegisterDriver shiftRegisterDriver)
        {
            switch (packet.DataType)
            {
                case PacketDataType.Lync:
                    using (var shiftRegisterSession = shiftRegisterDriver.AcquireSessionLock())
                    {
                        this.lyncCache.ProcessPacket(packet, shiftRegisterSession);
                    }

                    this.lyncCache.QueueDisplayStatus(this.lcdScreen);
                    break;
                default:
                    var page = this.lcdScreen.CreatePage();
                    page.Write(0, packet.Data);
                    this.lcdScreen.PushPage(page);
                    break;
            }
        }
    }
}
