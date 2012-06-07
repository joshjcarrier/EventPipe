namespace EventPipe.Common.ServiceBundle
{
    public class PluginServiceBundle
    {
        public ConfigurationService Configuration { get; set; }
        public EventAggregator Events { get; set; }
    }
}
