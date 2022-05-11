using Segment.Model;

namespace Snyk.Analytics
{
    using System;
    using Iteratively;
    using Segment;

    public class SegmentCustomDestination : IDestination
    {
        private readonly Client client;

        public SegmentCustomDestination(string writeKey, string anonymousId, string vsVersion = null)
        {
            Analytics.Initialize(writeKey, new Config()
                .SetAsync(true)
                .SetTimeout(TimeSpan.FromSeconds(10))
                .SetMaxQueueSize(5));
            this.client = Analytics.Client;

        }

        public void Dispose()
        {
            this.client.Dispose();
        }

        public void Init()
        {
        }

        public void Alias(string userId, string previousId)
        {
            this.client.Alias(previousId, userId);
        }

        public void Identify(string userId, Iteratively.Properties properties)
        {
            this.client.Identify(userId, properties.ToDictionary());
        }

        public void Group(string userId, string groupId, Iteratively.Properties properties)
        {
            this.client.Group(userId, groupId, properties.ToDictionary());
        }

        public void Track(string userId, string eventName, Iteratively.Properties properties)
        {
            this.client.Track(userId, eventName, properties.ToDictionary());
        }
    }
}