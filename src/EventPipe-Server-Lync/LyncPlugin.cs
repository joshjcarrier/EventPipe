namespace EventPipe.Server.Lync
{
    using EventPipe.Common;
    using EventPipe.Common.ServiceBundle;

    public class LyncPlugin : IPlugin
    {
        private readonly LyncService lyncService;

        public LyncPlugin(PluginServiceBundle serviceBundle)
        {
            this.lyncService = LyncService.Create(serviceBundle.Events);
        }

        public int BootOrder
        {
            get { return 9; }
        }

        public void Start()
        {
            this.lyncService.Start();
        }
    }
}
