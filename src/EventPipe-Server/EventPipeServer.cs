namespace EventPipe.Server
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using EventPipe.Common;
    using EventPipe.Common.Events;
    using EventPipe.Common.ServiceBundle;
    using EventPipe.Server.EventTransformer;
    using EventPipe.Server.Lync;
    using EventPipe.Server.SerialPort;

    public class EventPipeServer
    {
        private readonly NetduinoEventTransformer eventTransformer;
        private readonly EventAggregator eventAggregator;
        private readonly List<IPlugin> plugins;

        public EventPipeServer(NetduinoEventTransformer eventTransformer, EventAggregator eventAggregator)
        {
            this.eventTransformer = eventTransformer;
            this.eventAggregator = eventAggregator;
            this.plugins = new List<IPlugin>();
            
            this.RegisterPlugins(new[] { typeof(SerialPortPlugin), typeof(LyncPlugin) });

            this.eventTransformer.SubscribeTransformEvents(eventAggregator);
        }

        public static EventPipeServer Create(EventAggregator eventAggregator)
        {
            var transformer = NetduinoEventTransformer.Create(eventAggregator);
            return new EventPipeServer(transformer, eventAggregator);
        }

        public void Start()
        {
            foreach (var plugin in this.plugins.OrderBy(p => p.BootOrder))
            {
                plugin.Start();
            }
        }

        private IPlugin LoadPlugin(Type plugin)
        {
            // TODO weak reference the event aggregator and a better way to discover transformable events
            return (IPlugin)Activator.CreateInstance(plugin, new PluginServiceBundle { Events = this.eventAggregator });
        }

        private void RegisterPlugins(IEnumerable<Type> pluginManifest)
        {
            foreach (var pluginType in pluginManifest)
            {
                try
                {
                    this.plugins.Add(this.LoadPlugin(pluginType));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                    var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    this.eventAggregator.GetEvent<TraceEvent>().Publish(new TraceMessage { Owner = "SYSTEM", Message = pluginType.Name + " disabled. " + message });
                }
            }

            this.eventAggregator.GetEvent<TraceEvent>().Publish(
                new TraceMessage
                    {
                        Owner = "SYSTEM",
                        Message = "Loaded plugins: " + string.Join(string.Empty, this.plugins.OrderBy(p => p.GetType().Name).Select(p => Environment.NewLine + "\t\t\t[" + p.BootOrder + "] " + p.GetType().Name))
                    });
        }
    }
}
