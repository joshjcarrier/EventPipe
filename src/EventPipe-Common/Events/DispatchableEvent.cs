namespace EventPipe.Common.Events
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Threading;

    public abstract class DispatchableEvent<TPayload> : BaseEvent
    {
        private readonly Dispatcher dispatcher;
        private Dictionary<SubscriptionToken, Action<TPayload>> subscribers;

        protected DispatchableEvent(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.subscribers = new Dictionary<SubscriptionToken, Action<TPayload>>();
        }

        public void Publish(TPayload payload)
        {
            foreach(var subscriber in this.subscribers)
            {
                if (subscriber.Key.UseDispatcher)
                {
                    var subscriberCopy = subscriber;
                    this.dispatcher.Invoke(new Action(() => subscriberCopy.Value(payload)));
                    continue;
                }

                subscriber.Value(payload);
            }
        }

        public SubscriptionToken Subscribe(Action<TPayload> subscription)
        {
            return this.Subscribe(subscription, false);
        }

        public SubscriptionToken Subscribe(Action<TPayload> subscription, bool useDispatcher)
        {
            var subscriptionToken = new SubscriptionToken { UseDispatcher = useDispatcher };
            this.subscribers.Add(subscriptionToken, subscription);

            return subscriptionToken;
        }

        public class SubscriptionToken
        {
            public bool UseDispatcher { get; internal set; }
        }
    }
}
