namespace EventPipe.Server.CodeFlow
{
    using System;
    using System.Linq;
    using System.ServiceModel;
    using System.Threading;
    using EventPipe.Common;
    using EventPipe.Common.Data;
    using EventPipe.Common.Events;
    using EventPipe.Server.CodeFlow.ReviewService;

    internal class CodeFlowService
    {
        private readonly RawPublishEvent publishEvent;
        private readonly TraceEvent traceEvent;
        private readonly Thread codeFlowRefreshThread;
        private readonly int refreshInterval;
        private readonly int announceInterval;
        private readonly string authorWatch;
        private readonly string secondAuthorWatch;

        public CodeFlowService(
            string authorWatch,
            string secondAuthorWatch,
            int refreshInterval,
            int announceInterval,
            RawPublishEvent publishEvent,
            TraceEvent traceEvent)
        {
            this.authorWatch = authorWatch;
            this.secondAuthorWatch = secondAuthorWatch;
            this.refreshInterval = refreshInterval;
            this.announceInterval = announceInterval;
            this.publishEvent = publishEvent;
            this.traceEvent = traceEvent;
            this.codeFlowRefreshThread = new Thread(this.RunRefresh) { IsBackground = true };

            this.traceEvent.Publish(new TraceMessage { Owner = "CodeFlow", Message = "Ready" });
        }

        public static CodeFlowService Create(ConfigurationService configurationService, EventAggregator eventAggregator)
        {
            var authorWatch = configurationService["AuthorWatch"];
            var secondAuthorWatch = configurationService["SecondAuthorWatch"];
            var refreshInterval = int.Parse(configurationService["RefreshInterval"]);
            var announceInterval = int.Parse(configurationService["AnnounceInterval"]);
            var publishEventMessenger = eventAggregator.GetEvent<RawPublishEvent>();
            var traceEventMessenger = eventAggregator.GetEvent<TraceEvent>();
            return new CodeFlowService(authorWatch, secondAuthorWatch, refreshInterval, announceInterval, publishEventMessenger, traceEventMessenger);
        }

        public void Start()
        {
            this.codeFlowRefreshThread.Start();
        }

        private void RunRefresh()
        {
            try
            {
                this.traceEvent.Publish(new TraceMessage { Owner = "CodeFlow", Message = "Connecting to CodeFlow service" });
                
                var endpointAddressBuilder = new EndpointAddressBuilder();
                endpointAddressBuilder.Uri = new Uri("http://codeflow/Services/ReviewService.svc");
                endpointAddressBuilder.Identity = new DnsEndpointIdentity("localhost");
               
                // cache needs to be refreshed
                DateTime lastRefreshed = DateTime.MinValue;

                string payloadCache = null;
                while (true)
                {
                    if (DateTime.Now.Subtract(lastRefreshed).TotalMinutes > this.refreshInterval)
                    {
                        // try up to 3 times
                        for (var retries = 0; retries < 3; retries++)
                        {
                            try
                            {
                                this.traceEvent.Publish(new TraceMessage { Owner = "CodeFlow", Message = "Loading stats." });

                                // get a reference to the team project collection
                                using (var reviewServiceClient = new ReviewServiceClient(new WSHttpBinding(), endpointAddressBuilder.ToEndpointAddress()))
                                {
                                    reviewServiceClient.Open();
                                    var authorAssignedCount = reviewServiceClient.GetActiveReviewsForReviewer(this.authorWatch).Where(p => DateTime.Now.Subtract(p.LastUpdatedOn).TotalDays <= 7).Count();
                                    var authorCreatedCount = reviewServiceClient.GetActiveReviewsForAuthor(this.authorWatch).Where(p => DateTime.Now.Subtract(p.LastUpdatedOn).TotalDays <= 7).Count();
                                    var authorAssignedIndirectCount = reviewServiceClient.GetActiveReviewsForReviewer(this.secondAuthorWatch).Where(p => DateTime.Now.Subtract(p.LastUpdatedOn).TotalDays <= 7).Count();
                    
                                    payloadCache = string.Format(
                                        "{0} {1,-20}{3,-20}{2,-20}{4,-20}", 
                                        (char)PacketDataType.Text, 
                                        "CodeFlow Dashboard",
                                        "for " + this.authorWatch + ": " + authorAssignedCount,
                                        "by " + this.authorWatch + ": " + authorCreatedCount,
                                        "for " + this.secondAuthorWatch + ": " + authorAssignedIndirectCount);
                                }

                                this.traceEvent.Publish(new TraceMessage { Owner = "CodeFlow", Message = "Built stats: " + payloadCache });
                            }
                            catch (Exception ex)
                            {
                                this.traceEvent.Publish(new TraceMessage { Owner = "CodeFlow", Message = "Failed to refresh: " + ex.Message });
                                continue;
                            }

                            lastRefreshed = DateTime.Now;

                            this.traceEvent.Publish(new TraceMessage { Owner = "CodeFlow", Message = "Cached result." });
                            break;
                        }
                    }
                    else
                    {
                        this.traceEvent.Publish(new TraceMessage { Owner = "CodeFlow", Message = "Reloading cached." });
                    }

                    this.traceEvent.Publish(new TraceMessage { Owner = "CodeFlow", Message = "Broadcast result: " + payloadCache });
                    this.publishEvent.Publish(payloadCache);

                    // sleep i said
                    Thread.Sleep(this.announceInterval);
                }
            }
            catch (Exception ex)
            {
                this.traceEvent.Publish(new TraceMessage { Owner = "CodeFlow", Message = "Failure: " + ex.Message });
            }
        }
    }
}
