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
    using System.Configuration;

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

            var pluginManifest = ConfigurationManager.AppSettings.AllKeys
                .Where(p => p.StartsWith("plugin", StringComparison.OrdinalIgnoreCase))
                .Select(p => 
                {
                    var pluginData = ConfigurationManager.AppSettings[p].Split(';');
                    return new KeyValuePair<string, string>(pluginData[0], pluginData[1]);
                });
            
            // NOTE for now this requires adding to the probing path in the server app.config
            this.RegisterPlugins(pluginManifest);

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

        private IPlugin LoadPlugin(string pluginAssembly, string entryPoint)
        {
            // TODO weak reference the event aggregator
            // this resolution depends on the private bin path able to look in the plugins folder
            var objectHandle = Activator.CreateInstance(pluginAssembly, entryPoint);
            var plugin = (IPlugin)objectHandle.Unwrap();
            plugin.Initialize(new PluginServiceBundle { Events = this.eventAggregator });
            return plugin;
        }

        private void RegisterPlugins(IEnumerable<KeyValuePair<string, string>> pluginManifest)
        {
            foreach (var pluginType in pluginManifest)
            {
                try
                {
                    this.plugins.Add(this.LoadPlugin(pluginType.Key, pluginType.Value));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                    var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    var pluginName = pluginType.Value.Substring(pluginType.Value.LastIndexOf('.') + 1, pluginType.Value.Length - pluginType.Value.LastIndexOf('.') - 1);
                    this.eventAggregator.GetEvent<TraceEvent>().Publish(new TraceMessage { Owner = "SYSTEM", Message = pluginName + " disabled. " + message });
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
