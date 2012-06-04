namespace EventPipe.Common.Events
{
    using System.Windows.Threading;

    public class RawPublishEvent : DispatchableEvent<string>
    {
        public RawPublishEvent(Dispatcher dispatcher)
            : base(dispatcher)
        {
        }
    }
}
