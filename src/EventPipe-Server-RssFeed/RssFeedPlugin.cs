namespace EventPipe.Server.RssFeed
{
    using EventPipe.Common;
    using EventPipe.Common.ServiceBundle;

    public class RssFeedPlugin : IPlugin
    {
        private RssFeedService rssFeedService;

        public void Initialize(PluginServiceBundle serviceBundle)
        {
            this.rssFeedService = RssFeedService.Create(serviceBundle.Configuration, serviceBundle.Events);
        }

        public int BootOrder
        {
            get { return 7; }
        }

        public void Start()
        {
            this.rssFeedService.Start();
        }
    }
}
