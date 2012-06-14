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
        private readonly IEnumerable<SyndicationCache> feeds;
        private readonly int refreshInterval;
        private readonly int nextInterval;
        private readonly int maximumItems;
        private readonly RawPublishEvent publishEvent;
        private readonly TraceEvent traceEvent;

        public RssFeedService(IEnumerable<string> feeds, int refreshInterval, int nextInterval, int maximumItems, RawPublishEvent publishEvent, TraceEvent traceEvent)
        {
            this.refreshInterval = refreshInterval;
            this.nextInterval = nextInterval;
            this.maximumItems = maximumItems;
            this.publishEvent = publishEvent;
            this.traceEvent = traceEvent;

            this.feeds = feeds.Select(p =>
            {
                this.traceEvent.Publish(new TraceMessage { Owner = "RssFeed", Message = "Subscribing to feed: " + p });
                return new SyndicationCache(p);
            }).ToList();

            this.traceEvent.Publish(new TraceMessage { Owner = "RssFeed", Message = string.Format("Settings: Refresh: {0}m, Next interval: {1}ms, Max items: {2}", refreshInterval, nextInterval, maximumItems) });

            this.traceEvent.Publish(new TraceMessage { Owner = "RssFeed", Message = "Ready" });
        }

        public static RssFeedService Create(ConfigurationService configurationService, EventAggregator eventAggregator)
        {
            var feeds = configurationService.Where(p => p.Key.StartsWith("feed", StringComparison.OrdinalIgnoreCase)).Select(p => p.Value);
            var refreshInterval = int.Parse(configurationService["RefreshInterval"]);
            var nextInterval = int.Parse(configurationService["NextInterval"]);
            var maxItems = int.Parse(configurationService["MaximumItems"]);
            return new RssFeedService(feeds, refreshInterval, nextInterval, maxItems, eventAggregator.GetEvent<RawPublishEvent>(), eventAggregator.GetEvent<TraceEvent>());
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
                foreach (var feed in this.feeds)
                {
                    // cache needs to be refreshed
                    if (DateTime.Now.Subtract(feed.LastRefreshed).TotalMinutes > this.refreshInterval)
                    {
                        // try up to 3 times
                        for (var retries = 0; retries < 3; retries++)
                        {
                            var cache = new List<string>();

                            try
                            {
                                this.traceEvent.Publish(new TraceMessage { Owner = "RssFeed", Message = "Loading feed: " + feed.Source });

                                var reader = XmlReader.Create(feed.Source);
                                var feedReader = SyndicationFeed.Load(reader);

                                foreach (var item in feedReader.Items.Take(this.maximumItems))
                                {
                                    var payload = (char)PacketDataType.Text + " " + string.Format("{0,-20}{1}", feedReader.Title.Text.Substring(0, Math.Min(20, feedReader.Title.Text.Length)), item.Title.Text);
                                    cache.Add(payload);
                                }
                            }
                            catch (Exception ex)
                            {
                                this.traceEvent.Publish(new TraceMessage { Owner = "RssFeed", Message = "Failed to load: " + feed.Source + " " + ex.Message });
                                continue;
                            }

                            feed.LastRefreshed = DateTime.Now;
                            feed.PayloadCache = cache;
                            this.traceEvent.Publish(new TraceMessage { Owner = "RssFeed", Message = "Cached feed: " + feed.Source });
                            break;
                        }
                    }
                    else
                    {
                        this.traceEvent.Publish(new TraceMessage { Owner = "RssFeed", Message = "Reloading cached feed: " + feed.Source });
                    }

                    foreach (var cacheItem in feed.PayloadCache)
                    {
                        this.traceEvent.Publish(new TraceMessage { Owner = "RssFeed", Message = "Broadcast item: " + feed.Source + " - " + cacheItem });
                        this.publishEvent.Publish(cacheItem);

                        // sleep i said
                        Thread.Sleep(this.nextInterval);
                    }
                }
            }
        }

        private class SyndicationCache
        {
            public SyndicationCache(string feed)
            {
                this.Source = feed;
                this.LastRefreshed = DateTime.MinValue;
                this.PayloadCache = new List<string>();
            }

            public string Source { get; private set; }

            public DateTime LastRefreshed { get; set; }

            public List<string> PayloadCache { get; set; }
        }
    }
}
