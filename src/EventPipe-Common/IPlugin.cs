namespace EventPipe.Common
{
    using EventPipe.Common.ServiceBundle;

    public interface IPlugin
    {
        int BootOrder { get; }

        void Initialize(PluginServiceBundle serviceBundle);

        void Start();
    }
}
