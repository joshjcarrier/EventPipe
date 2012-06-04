namespace EventPipe.Server.EventMessaging
{
    using System.Windows.Threading;

    public class TraceEventMessenger : DispatchableEventMessenger<TraceMessage>
    {
        internal TraceEventMessenger(Dispatcher dispatcher) : base(dispatcher)
        {
        }
    }
}
