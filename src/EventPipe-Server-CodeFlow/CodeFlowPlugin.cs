namespace EventPipe.Server.CodeFlow
{
    using EventPipe.Common;
    using EventPipe.Common.ServiceBundle;

    public class CodeFlowPlugin : IPlugin
    {
        private CodeFlowService codeFlowService;
        
        public void Initialize(PluginServiceBundle serviceBundle)
        {
            this.codeFlowService = CodeFlowService.Create(serviceBundle.Configuration, serviceBundle.Events);
        }

        public int BootOrder
        {
            get { return 9; }
        }

        public void Start()
        {
            this.codeFlowService.Start();
        }
    }
}
