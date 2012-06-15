namespace EventPipe.Server.TfsClient
{
    using EventPipe.Common;
    using EventPipe.Common.ServiceBundle;

    public class TfsClientPlugin : IPlugin
    {
        private TfsClientService tfsClientService;

        public void Initialize(PluginServiceBundle serviceBundle)
        {
            // TODO hold on to this for proper initialization, disposal
            this.tfsClientService = TfsClientService.Create(serviceBundle.Configuration, serviceBundle.Events);
        }

        public int BootOrder
        {
            get { return 8; }
        }

        public void Start()
        {
            this.tfsClientService.Start();
        }
    }
}
