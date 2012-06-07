namespace EventPipe.Server.Lync
{
    using EventPipe.Common;
    using EventPipe.Common.ServiceBundle;

    public class LyncPlugin : IPlugin
    {
        private LyncService lyncService;
        
        public void Initialize(PluginServiceBundle serviceBundle)
        {
            this.lyncService = LyncService.Create(serviceBundle.Configuration, serviceBundle.Events);
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
