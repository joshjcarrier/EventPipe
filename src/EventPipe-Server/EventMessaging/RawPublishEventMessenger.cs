namespace EventPipe.Server.EventMessaging
{
    using System.Windows.Threading;

    public class RawPublishEventMessenger : DispatchableEventMessenger<string>
    {
        internal RawPublishEventMessenger(Dispatcher dispatcher)
            : base(dispatcher)
        {
        }
    }
}
