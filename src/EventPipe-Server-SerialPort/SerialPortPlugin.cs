namespace EventPipe.Server.SerialPort
{
    using System.Configuration;
    using System.IO.Ports;
    using EventPipe.Server.EventMessaging;

    public class SerialPortPlugin
    {
        private readonly SerialPort serialPort;
        private readonly TraceEventMessenger traceEventMessenger;

        public SerialPortPlugin(string serialPortName, RawPublishEventMessenger publishEventMessenger, TraceEventMessenger traceEventMessenger)
        {
            this.traceEventMessenger = traceEventMessenger;

            this.serialPort = new SerialPort(serialPortName, 9600, Parity.None, 8, StopBits.One);
            this.serialPort.Open();

            this.traceEventMessenger.Publish(new TraceMessage { Owner = "SerialPort", Message =  "Opened " + serialPortName });
            publishEventMessenger.Subscribe(this.WriteRawPacket);
            this.traceEventMessenger.Publish(new TraceMessage { Owner = "SerialPort", Message = "Ready" });
        }
        
        public static SerialPortPlugin Create(EventAggregator eventAggregator)
        {
            var serialPortName = ConfigurationManager.AppSettings["SerialPortName"];
            var publishEventMessenger = eventAggregator.GetEvent<RawPublishEventMessenger>();
            var traceEventMessenger = eventAggregator.GetEvent<TraceEventMessenger>();
            return new SerialPortPlugin(serialPortName, publishEventMessenger, traceEventMessenger);   
        }

        public void WriteRawPacket(string payload)
        {
            this.traceEventMessenger.Publish(new TraceMessage { Owner = "SerialPort", Message = "Begin TX: " + payload });
            this.serialPort.Write(payload);
            this.traceEventMessenger.Publish(new TraceMessage { Owner = "SerialPort", Message = "End TX: " + payload });
        }
    }
}
