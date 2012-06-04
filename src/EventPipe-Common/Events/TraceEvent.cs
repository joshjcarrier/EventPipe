namespace EventPipe.Common.Events
{
    using System.Windows.Threading;

    public class TraceEvent : DispatchableEvent<TraceMessage>
    {
        public TraceEvent(Dispatcher dispatcher) : base(dispatcher)
        {
        }
    }
}
