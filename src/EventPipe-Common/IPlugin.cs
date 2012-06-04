namespace EventPipe.Common
{
    public interface IPlugin
    {
        int BootOrder { get; }

        void Start();
    }
}
