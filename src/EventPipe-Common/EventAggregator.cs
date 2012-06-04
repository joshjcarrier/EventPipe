namespace EventPipe.Common
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Threading;
    using EventPipe.Common.Events;

    public class EventAggregator
    {
        private readonly Dictionary<Type, object> eventMessengerInstances;
        private readonly Dispatcher dispatcher;

        public EventAggregator(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.eventMessengerInstances = new Dictionary<Type, object>();
        }

        public TEventType GetEvent<TEventType>() where TEventType : BaseEvent
        {
            object eventAggregate;
            if (!this.eventMessengerInstances.TryGetValue(typeof(TEventType), out eventAggregate))
            {
                eventAggregate = Activator.CreateInstance(typeof(TEventType), new[] { this.dispatcher });
                this.eventMessengerInstances[typeof(TEventType)] = eventAggregate;
            }

            return (TEventType)eventAggregate;
        }
    }
}
