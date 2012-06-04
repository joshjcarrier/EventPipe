namespace EventPipe.Server
{
    using System;
    using System.Diagnostics;
    using EventPipe.Common;
    using EventPipe.Common.Events;
    using EventPipe.Server.EventTransformer;
    using EventPipe.Server.Lync;
    using EventPipe.Server.Lync.Events;
    using EventPipe.Server.SerialPort;

    public class EventPipeServer
    {
        private readonly NetduinoEventTransformer eventTransformer;

        public EventPipeServer(NetduinoEventTransformer eventTransformer, EventAggregator eventAggregator)
        {
            this.eventTransformer = eventTransformer;

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
                // TODO how to know this is the event we want?
                eventAggregator.GetEvent<LyncStatusEvent>().Subscribe(this.eventTransformer.Transform);
                LyncPlugin.Create(eventAggregator);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                eventAggregator.GetEvent<TraceEvent>().Publish(new TraceMessage { Owner = "SYSTEM", Message = "Lync integration disabled. " + ex.Message });
            }
        }

        public static EventPipeServer Create(EventAggregator eventAggregator)
        {
            var transformer = NetduinoEventTransformer.Create(eventAggregator);
            return new EventPipeServer(transformer, eventAggregator);
        }
    }
}
