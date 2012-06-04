﻿namespace EventPipe.Server.SerialPort
{
    using System.Configuration;
    using System.IO.Ports;
    using EventPipe.Common;
    using EventPipe.Common.Events;

    internal class SerialPortService
    {
        private readonly SerialPort serialPort;
        private readonly TraceEvent traceEvent;

        public SerialPortService(string serialPortName, RawPublishEvent publishEvent, TraceEvent traceEvent)
        {
            this.traceEvent = traceEvent;

            this.serialPort = new SerialPort(serialPortName, 9600, Parity.None, 8, StopBits.One);
            this.serialPort.Open();

            this.traceEvent.Publish(new TraceMessage { Owner = "SerialPort", Message =  "Opened " + serialPortName });
            publishEvent.Subscribe(this.WriteRawPacket);
            this.traceEvent.Publish(new TraceMessage { Owner = "SerialPort", Message = "Ready" });
        }

        public static SerialPortService Create(EventAggregator eventAggregator)
        {
            var serialPortName = ConfigurationManager.AppSettings["SerialPortName"];
            var publishEventMessenger = eventAggregator.GetEvent<RawPublishEvent>();
            var traceEventMessenger = eventAggregator.GetEvent<TraceEvent>();
            return new SerialPortService(serialPortName, publishEventMessenger, traceEventMessenger);   
        }

        public void Start()
        {
            // everything is already ready to go
        }

        public void WriteRawPacket(string payload)
        {
            this.traceEvent.Publish(new TraceMessage { Owner = "SerialPort", Message = "Begin TX: " + payload });
            this.serialPort.Write(payload);
            this.traceEvent.Publish(new TraceMessage { Owner = "SerialPort", Message = "End TX: " + payload });
        }
    }
}
