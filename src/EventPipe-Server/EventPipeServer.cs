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

            var pluginManifest = ConfigurationManager.AppSettings["plugins"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            
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

        private IPlugin LoadPlugin(string pluginAssembly, string entryPoint, ConfigurationService configurationService)
        {
            // TODO weak reference the event aggregator
            // this resolution depends on the private bin path able to look in the plugins folder
            // TODO load plugins into separate app domains
            var objectHandle = Activator.CreateInstance(pluginAssembly, entryPoint);
            var plugin = (IPlugin)objectHandle.Unwrap();
            plugin.Initialize(new PluginServiceBundle { Configuration = configurationService, Events = this.eventAggregator });
            return plugin;
        }

        private void RegisterPlugins(IEnumerable<string> pluginManifest)
        {
            foreach (var pluginRootFolder in pluginManifest)
            {
                try
                {
                    var fileMap = new ExeConfigurationFileMap() { ExeConfigFilename = "plugins/" + pluginRootFolder + "/plugin.config" };
                    var configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

                    var entryPointConfig = configuration.AppSettings.Settings["EntryPoint"];
                    if (!configuration.HasFile || entryPointConfig == null)
                    {
                        this.eventAggregator.GetEvent<TraceEvent>().Publish(new TraceMessage { Owner = "SYSTEM", Message = pluginRootFolder + " disabled. Could not load plugin configuration." });
                        continue;
                    }

                    var configurationService = new ConfigurationService();
                    foreach (KeyValueConfigurationElement configItem in configuration.AppSettings.Settings)
                    {
                        configurationService[configItem.Key] = configItem.Value;
                    }

                    // TODO insert shared configuration selectively
                    var entryPoint = entryPointConfig.Value.Split(':');
                    this.plugins.Add(this.LoadPlugin(entryPoint[0], entryPoint[1], configurationService));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                    var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    this.eventAggregator.GetEvent<TraceEvent>().Publish(new TraceMessage { Owner = "SYSTEM", Message = pluginRootFolder + " disabled. " + message });
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
