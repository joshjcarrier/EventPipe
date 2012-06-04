namespace EventPipe.Server
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Threading;
    using EventPipe.Server.EventMessaging;

    public class EventAggregator
    {
        private readonly Dictionary<Type, object> eventMessengerInstances;

        public EventAggregator(Dispatcher dispatcher)
        {
            this.eventMessengerInstances = new Dictionary<Type, object>();

            // event messengers that require dispatchers populate here
            this.eventMessengerInstances[typeof(RawPublishEventMessenger)] = new RawPublishEventMessenger(dispatcher);
            this.eventMessengerInstances[typeof(TraceEventMessenger)] = new TraceEventMessenger(dispatcher);
        }

        public TEventType GetEvent<TEventType>() where TEventType : BaseEventMessenger
        {
            object eventAggregate;
            if (!this.eventMessengerInstances.TryGetValue(typeof(TEventType), out eventAggregate))
            {
                eventAggregate = Activator.CreateInstance(typeof(TEventType));
                this.eventMessengerInstances[typeof(TEventType)] = eventAggregate;
            }

            return (TEventType)eventAggregate;
        }
    }
}
