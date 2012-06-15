namespace EventPipe.Server.TfsClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using EventPipe.Common;
    using EventPipe.Common.Data;
    using EventPipe.Common.Events;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.WorkItemTracking.Client;

    internal class TfsClientService
    {
        private readonly string projectCollectionUrl;
        private readonly string projectName;
        private readonly IEnumerable<string> serverCountQueries;
        private readonly RawPublishEvent publishEvent;
        private readonly TraceEvent traceEvent;
        private readonly Thread tfsClientRefreshThread;
        private readonly int refreshInterval;
        private readonly int nextInterval;

        public TfsClientService(
            string projectCollectionUrl, 
            string projectName, 
            IEnumerable<string> serverCountQueries, 
            int refreshInterval,
            int nextInterval,
            RawPublishEvent publishEvent, 
            TraceEvent traceEvent)
        {
            this.projectCollectionUrl = projectCollectionUrl;
            this.projectName = projectName;
            this.serverCountQueries = serverCountQueries;
            this.refreshInterval = refreshInterval;
            this.nextInterval = nextInterval;
            this.publishEvent = publishEvent;
            this.traceEvent = traceEvent;
            this.tfsClientRefreshThread = new Thread(this.RunTfsClientRefresh) { IsBackground = true };

            this.traceEvent.Publish(new TraceMessage { Owner = "TfsClient", Message = string.Format("Project collection url: {0}, Project name: {1}", projectCollectionUrl, projectName) });

            this.traceEvent.Publish(new TraceMessage { Owner = "TfsClient", Message = "Ready" });
        }

        public static TfsClientService Create(ConfigurationService configurationService, EventAggregator eventAggregator)
        {
            var projectCollectionUrl = configurationService["ProjectCollectionUrl"];
            var projectName = configurationService["ProjectName"];
            var serverCountQueries = configurationService.Where(p => p.Key.StartsWith("query", StringComparison.OrdinalIgnoreCase)).Select(p => p.Value);
            var refreshInterval = int.Parse(configurationService["RefreshInterval"]);
            var nextInterval = int.Parse(configurationService["NextInterval"]);
            var publishEventMessenger = eventAggregator.GetEvent<RawPublishEvent>();
            var traceEventMessenger = eventAggregator.GetEvent<TraceEvent>();
            return new TfsClientService(projectCollectionUrl, projectName, serverCountQueries, refreshInterval, nextInterval, publishEventMessenger, traceEventMessenger);   
        }

        public void Start()
        {
            this.tfsClientRefreshThread.Start();
        }

        private void RunTfsClientRefresh()
        {
            try
            {
                this.traceEvent.Publish(new TraceMessage { Owner = "TfsClient", Message = "Connecting to TFS server" });
                // get a reference to the team project collection
                using (var projectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(this.projectCollectionUrl)))
                {
                    // get a reference to the work item tracking service
                    var workItemStore = projectCollection.GetService<WorkItemStore>();

                    if (workItemStore.Projects.Count <= 0)
                    {
                        this.traceEvent.Publish(new TraceMessage { Owner = "TfsClient", Message = "There are no projects in this server" });
                        return;
                    }

                    var projectWithQueries =
                        workItemStore.Projects.Cast<Project>().Where(project => project.Name == this.projectName).Single();

                    if (projectWithQueries == null)
                    {
                        this.traceEvent.Publish(new TraceMessage { Owner = "TfsClient", Message = "There are no projects with stored queries" });
                        return;
                    }

                    this.traceEvent.Publish(new TraceMessage { Owner = "TfsClient", Message = "Retrieving queries" });

                    var definitions = GetQueryDefinitions(projectWithQueries.QueryHierarchy)
                        .Where(p => this.serverCountQueries.Contains(p.Path, StringComparer.InvariantCultureIgnoreCase))
                        .Select(p => new TfsQueryCache(p))
                        .ToList();
                    
                    if (!definitions.Any())
                    {
                        this.traceEvent.Publish(new TraceMessage { Owner = "TfsClient", Message = "There are selected queries" });
                        return;
                    }

                    var variables = new Dictionary<string, string> { { "project", this.projectName } };
                    while (true)
                    {
                        foreach (var definition in definitions)
                        {
                            // cache needs to be refreshed
                            if (DateTime.Now.Subtract(definition.LastRefreshed).TotalMinutes > this.refreshInterval)
                            {
                                // try up to 3 times
                                for (var retries = 0; retries < 3; retries++)
                                {
                                    try
                                    {
                                        this.traceEvent.Publish(new TraceMessage { Owner = "TfsClient", Message = "Loading query: " + definition.Query.Path });
                                        
                                        var result = workItemStore.QueryCount(definition.Query.QueryText, variables);
                                        definition.PayloadCache = string.Format("{0} {1,-40}{2,18}", (char)PacketDataType.Text, definition.Query.Name, result);

                                        this.traceEvent.Publish(new TraceMessage { Owner = "TfsClient", Message = definition.Query.Path + ": " + result });
                                    }
                                    catch (Exception ex)
                                    {
                                        this.traceEvent.Publish(new TraceMessage { Owner = "TfsClient", Message = "Failed to load: " + definition.Query.Path + " " + ex.Message });
                                        continue;
                                    }

                                    definition.LastRefreshed = DateTime.Now;

                                    this.traceEvent.Publish(new TraceMessage { Owner = "TfsClient", Message = "Cached result: " + definition.Query.Path });
                                    break;
                                }
                            }
                            else
                            {
                                this.traceEvent.Publish(new TraceMessage { Owner = "TfsClient", Message = "Reloading cached feed: " + definition.Query.Path });
                            }

                            var payload = definition.PayloadCache;
                            this.traceEvent.Publish(new TraceMessage { Owner = "TfsClient", Message = "Broadcast result: " + definition.Query.Path + " - " + payload });
                            this.publishEvent.Publish(payload);

                            // sleep i said
                            Thread.Sleep(this.nextInterval);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.traceEvent.Publish(new TraceMessage { Owner = "TfsClient", Message = "Failure: " + ex.Message });
            }
        }

        private static IEnumerable<QueryDefinition> GetQueryDefinitions(QueryFolder rootFolder)
        {
            var queryDefinitions = new List<QueryDefinition>();
            foreach (QueryItem queryItem in rootFolder)
            {
                var subFolder = queryItem as QueryFolder;
                if (subFolder != null)
                {
                    queryDefinitions.AddRange(GetQueryDefinitions(subFolder));
                }

                var queryDefinition = queryItem as QueryDefinition;
                if (queryDefinition != null)
                {
                    queryDefinitions.Add(queryDefinition);
                }
            }

            return queryDefinitions;
        }

        private class TfsQueryCache
        {
            public TfsQueryCache(QueryDefinition query)
            {
                this.Query = query;
                this.LastRefreshed = DateTime.MinValue;
                this.PayloadCache = string.Empty;
            }

            public QueryDefinition Query { get; private set; }

            public DateTime LastRefreshed { get; set; }

            public string PayloadCache { get; set; }
        }
    }
}
