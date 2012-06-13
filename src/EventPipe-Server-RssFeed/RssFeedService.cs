namespace EventPipe.Server.RssFeed
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceModel.Syndication;
    using System.Threading;
    using System.Xml;
    using EventPipe.Common;
    using EventPipe.Common.Data;
    using EventPipe.Common.Events;

    internal class RssFeedService
    {
        private readonly IEnumerable<string> feeds;
        private readonly int refreshInterval;
        private readonly int nextInterval;
        private readonly RawPublishEvent publishEvent;
        private readonly TraceEvent traceEvent;

        public RssFeedService(IEnumerable<string> feeds, int refreshInterval, int nextInterval, RawPublishEvent publishEvent, TraceEvent traceEvent)
        {
            this.feeds = feeds;
            this.refreshInterval = refreshInterval;
            this.nextInterval = nextInterval;
            this.publishEvent = publishEvent;
            this.traceEvent = traceEvent;

            this.traceEvent.Publish(new TraceMessage { Owner = "RssFeed", Message = "Ready" });
        }

        public static RssFeedService Create(ConfigurationService configurationService, EventAggregator eventAggregator)
        {
            var feeds = configurationService.Where(p => p.Key.StartsWith("feed", StringComparison.OrdinalIgnoreCase)).Select(p => p.Value);
            var refreshInterval = int.Parse(configurationService["RefreshInterval"]);
            var nextInterval = int.Parse(configurationService["NextInterval"]);
            return new RssFeedService(feeds, refreshInterval, nextInterval, eventAggregator.GetEvent<RawPublishEvent>(), eventAggregator.GetEvent<TraceEvent>());
        }

        public void Start()
        {
            var syndicationFeedPublisherThread = new Thread(this.RunSyndicationFeedPublisher) { IsBackground = true, Priority = ThreadPriority.BelowNormal };
            syndicationFeedPublisherThread.Start();
        }

        private void RunSyndicationFeedPublisher()
        {
            while (true)
            {
                var cache = new List<string>();
                foreach (var feed in this.feeds)
                {
                    var reader = XmlReader.Create(feed);
                    var feedReader = SyndicationFeed.Load(reader);

                    foreach (var item in feedReader.Items)
                    {
                        var payload = (char)PacketDataType.Text + " " + string.Format("{0,-20}{1}", feedReader.Title.Text.Substring(0, Math.Min(20, feedReader.Title.Text.Length)), item.Title.Text);
                        cache.Add(payload);
                    }

                    this.traceEvent.Publish(new TraceMessage { Owner = "RssFeed", Message = "Cached " + feed });
                }

                var lastRefreshed = DateTime.Now;
                while (DateTime.Now.Subtract(lastRefreshed).TotalMinutes < this.refreshInterval)
                {
                    foreach (var cacheItem in cache)
                    {
                        this.publishEvent.Publish(cacheItem);

                        // refetch feeds if it's time to
                        if (DateTime.Now.Subtract(lastRefreshed).TotalMinutes > this.refreshInterval)
                        {
                            break;
                        }

                        // sleep i said
                        Thread.Sleep(this.nextInterval);
                    }
                }
            }
        }
    }
}
