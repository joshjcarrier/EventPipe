namespace EventPipe.Server.EventTransformer
{
    using System;
    using System.Diagnostics;
    using EventPipe.Common;
    using EventPipe.Common.Events;
    using EventPipe.Server.Lync;
    using EventPipe.Server.Lync.Events;
    using EventPipe.Server.SerialPort;

    public class NetduinoEventTransformer
    {
        private readonly EventAggregator eventAggregator;

        public NetduinoEventTransformer(EventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;

            // TODO initialize and manage this properly
            try
            {
                SerialPortPlugin.Create(eventAggregator);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                eventAggregator.GetEvent<TraceEvent>().Publish(new TraceMessage { Owner = "SYSTEM", Message = "Serial port plugin disabled. " + ex.Message });
            }

            try
            {
                eventAggregator.GetEvent<LyncStatusEvent>().Subscribe(this.Transform);
                LyncPlugin.Create(eventAggregator);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                eventAggregator.GetEvent<TraceEvent>().Publish(new TraceMessage { Owner = "SYSTEM", Message = "Lync integration disabled. " + ex.Message });
            }
        }

        public static NetduinoEventTransformer Create(EventAggregator eventAggregator)
        {
            return new NetduinoEventTransformer(eventAggregator);
        }

        private void Transform(LyncStatusChange lyncStatusChange)
        {
            this.eventAggregator.GetEvent<TraceEvent>().Publish(new TraceMessage { Owner = "SYSTEM", Message = "Transform and emit Lync status as Netduino payload: " + lyncStatusChange });
            this.eventAggregator.GetEvent<RawPublishEvent>().Publish(lyncStatusChange.Status);
        }
    }
}
