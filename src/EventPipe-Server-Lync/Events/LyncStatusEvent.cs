namespace EventPipe.Server.Lync.Events
{
    using System.Windows.Threading;
    using EventPipe.Common.Events;

    public class LyncStatusEvent : DispatchableEvent<LyncStatusChange>
    {
        public LyncStatusEvent(Dispatcher dispatcher) 
            : base(dispatcher)
        {
        }
    }
}
