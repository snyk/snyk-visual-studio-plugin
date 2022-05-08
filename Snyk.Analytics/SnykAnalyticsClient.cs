using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Iteratively;
using Segment;
using Segment.Model;
using Snyk.Common;
using ILogger = Serilog.ILogger;

namespace Snyk.Analytics
{
    public class SnykAnalyticsClient : ISnykAnalyticsService
    {
        private readonly string anonymousId;
        private readonly string writeKey;
        private bool enabled = true;
        private string userId;
        private string userIdAsHash;
        private readonly Client segmentClient;

        private static readonly ILogger Logger = LogManager.ForContext<SnykAnalyticsClient>();

        private SnykAnalyticsClient(string anonymousId, string writeKey, Client segmentClient)
        {
            this.anonymousId = anonymousId;
            this.writeKey = writeKey;
            this.segmentClient = segmentClient ?? throw new InvalidOperationException("Segment client not initialized");
            this.segmentClient.Identify(this.anonymousId, new Traits());
        }

        public static SnykAnalyticsClient Instance { get; private set; }

        public bool AnalyticsEnabled { get; set; } = true;

        public static void Initialize(string anonymousId, string writeKey)
        {
            Segment.Analytics.Initialize(writeKey, new Config()
                .SetAsync(true)
                .SetTimeout(TimeSpan.FromSeconds(10))
                .SetMaxQueueSize(5));
            Instance = new SnykAnalyticsClient(anonymousId, writeKey, Segment.Analytics.Client);
        }

        public void LogAnalysisReadyEvent(string analysisType, string analysisResult)
        {
            throw new NotImplementedException();
        }

        public void LogWelcomeIsViewedEvent()
        {
            throw new NotImplementedException();
        }

        public void LogAnalysisIsTriggeredEvent(IList<string> selectedProducts)
        {
            throw new NotImplementedException();
        }

        public void LogIssueIsViewedEvent(string id, string issueType, string severity)
        {
            throw new NotImplementedException();
        }

        public void ObtainUser(string apiToken)
        {
            throw new NotImplementedException();
        }

        public async Task ObtainUserAsync(string apiToken)
        {
            if (string.IsNullOrEmpty(apiToken) || !string.IsNullOrEmpty(this.userId) || !this.AnalyticsEnabled)
            {
                return;
            }

            var user = await SnykUser.GetUserAsync(apiToken);

            if (string.IsNullOrEmpty(user.Id))
            {
                Logger.Information("Alias event cannot be executed because userId is empty");
                return;
            }

            this.userId = user.Id;

            this.segmentClient.Alias(this.anonymousId, userId);
        }

        public string UserIdAsHash
        {
            get
            {
                if (string.IsNullOrEmpty(this.userIdAsHash) && !string.IsNullOrEmpty(this.userId))
                {
                    this.userIdAsHash = Sha256.ComputeHash(this.userId);
                }

                return this.userIdAsHash;
            }
        }

        public void Dispose()
        {
        }
    }
}
