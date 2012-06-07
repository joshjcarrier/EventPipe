namespace EventPipe.Server.SerialPort
{
    using EventPipe.Common;
    using EventPipe.Common.ServiceBundle;

    public class SerialPortPlugin : IPlugin
    {
        private SerialPortService serialPortService;

        public void Initialize(PluginServiceBundle serviceBundle)
        {
            // TODO hold on to this for proper initialization, disposal
            this.serialPortService = SerialPortService.Create(serviceBundle.Configuration, serviceBundle.Events);
        }

        public int BootOrder
        {
            get { return 1; }
        }

        public void Start()
        {
            this.serialPortService.Start();
        }
    }
}
