namespace EventPipe.Server.SerialPort
{
    using System.Collections.Concurrent;
    using System.IO.Ports;
    using System.Threading;
    using EventPipe.Common;
    using EventPipe.Common.Events;

    internal class SerialPortService
    {
        private readonly SerialPort serialPort;
        private readonly TraceEvent traceEvent;
        private readonly Thread writeRawPacketThread;
        private readonly BlockingCollection<string> rawPacketOutBuffer; 

        public SerialPortService(string serialPortName, RawPublishEvent publishEvent, TraceEvent traceEvent)
        {
            this.traceEvent = traceEvent;
            this.rawPacketOutBuffer = new BlockingCollection<string>();

            this.serialPort = new SerialPort(serialPortName, 9600, Parity.None, 8, StopBits.One);
            this.serialPort.Open();

            this.writeRawPacketThread = new Thread(this.RunWriteRawPacket) { IsBackground = true };

            this.traceEvent.Publish(new TraceMessage { Owner = "SerialPort", Message = "Opened " + serialPortName });
            publishEvent.Subscribe(this.WriteRawPacket);
            this.traceEvent.Publish(new TraceMessage { Owner = "SerialPort", Message = "Ready" });
        }

        public static SerialPortService Create(ConfigurationService configurationService, EventAggregator eventAggregator)
        {
            var serialPortName = configurationService["SerialPortName"];
            var publishEventMessenger = eventAggregator.GetEvent<RawPublishEvent>();
            var traceEventMessenger = eventAggregator.GetEvent<TraceEvent>();
            return new SerialPortService(serialPortName, publishEventMessenger, traceEventMessenger);   
        }

        public void Start()
        {
            this.writeRawPacketThread.Start();
        }

        public void WriteRawPacket(string payload)
        {
            this.rawPacketOutBuffer.Add(payload);
        }

        public void RunWriteRawPacket()
        {
            while (true)
            {
                var payload = this.rawPacketOutBuffer.Take();

                this.traceEvent.Publish(new TraceMessage { Owner = "SerialPort", Message = "TX: " + payload });
                this.serialPort.WriteLine(payload);    
            }
        }
    }
}
