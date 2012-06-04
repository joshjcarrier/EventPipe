namespace EventPipe.Server.EventTransformer
{
    using EventPipe.Common;
    using EventPipe.Common.Events;
    using EventPipe.Common.Events.Lync;

    public class NetduinoEventTransformer
    {
        private readonly RawPublishEvent publishEvent;
        private readonly TraceEvent traceEvent;

        public NetduinoEventTransformer(RawPublishEvent publishEvent, TraceEvent traceEvent)
        {
            this.publishEvent = publishEvent;
            this.traceEvent = traceEvent;
        }

        public static NetduinoEventTransformer Create(EventAggregator eventAggregator)
        {
            return new NetduinoEventTransformer(eventAggregator.GetEvent<RawPublishEvent>(), eventAggregator.GetEvent<TraceEvent>());
        }

        public void SubscribeTransformEvents(EventAggregator eventAggregator)
        {
            // decide which events should be transformed
            eventAggregator.GetEvent<LyncStatusEvent>().Subscribe(this.Transform);
        }

        public void Transform(LyncStatusChange lyncStatusChange)
        {
            this.traceEvent.Publish(new TraceMessage { Owner = "SYSTEM", Message = "Transform and emit Lync status as Netduino payload: " + lyncStatusChange });
            this.publishEvent.Publish(lyncStatusChange.Status);
        }

        public void Transform(object eventObject)
        {
            this.traceEvent.Publish(new TraceMessage { Owner = "SYSTEM", Message = "Transformation unknown for event: " + eventObject});
        }
    }
}
