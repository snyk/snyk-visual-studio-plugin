namespace Snyk.Analytics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Iteratively;
    using Segment;
    using Segment.Model;
    using Snyk.Common;
    using Guid = System.Guid;
    using ILogger = Serilog.ILogger;

    public class SnykAnalyticsService : ISnykAnalyticsService
    {
        private readonly string anonymousId;
        private readonly Client segmentClient;
        private readonly Uri userMeEndpoint;
        private static readonly ILogger Logger = LogManager.ForContext<SnykAnalyticsService>();
        private string userId;
        private string vsVersion;
        private string userIdAsHash;
        private bool _analyticsEnabled;

        private SnykAnalyticsService(string anonymousId, Client segmentClient, Uri userMeEndpoint)
        {
            this.anonymousId = anonymousId;
            this.segmentClient = segmentClient;
            this.userMeEndpoint = userMeEndpoint;
        }

        public static SnykAnalyticsService Instance { get; private set; }

        public bool AnalyticsEnabled
        {
            get => _analyticsEnabled;
            set
            {
                if (_analyticsEnabled == value)
                {
                    return;
                }

                _analyticsEnabled = value;
                
                if (value)
                {
                    Logger.Information("Analytics enabled");
                }
                else
                {
                    Logger.Information("Analytics disabled");
                }
            }
        }

        private bool Disabled => !AnalyticsEnabled || this.segmentClient == null;

        public static void Initialize(string anonymousId, string writeKey, bool enabled, string userMeApiEndpoint)
        {
            if (string.IsNullOrEmpty(anonymousId))
            {
                anonymousId = Guid.NewGuid().ToString();
            }

            Uri endpoint;
            try
            {
                endpoint = new Uri(userMeApiEndpoint);
            }
            catch (Exception exception)
            {
                Logger.Error($"Analytics disabled because of invalid api endpoint: \"{userMeApiEndpoint}\"");
                Logger.Error(exception.Message);
                Instance = new SnykAnalyticsService(anonymousId, null, null);
                Instance.AnalyticsEnabled = false;
                return;
            }

            if (string.IsNullOrEmpty(writeKey))
            {
                Instance = new SnykAnalyticsService(anonymousId, null, endpoint);
                Instance.AnalyticsEnabled = false;
                Logger.Information("Analytics disabled because of empty write key");
                return;
            }

            var segmentDestination = new SegmentCustomDestination(writeKey);
            Itly.Load(new Iteratively.Options(new DestinationsOptions(new CustomOptions(segmentDestination))));
            
            Instance = new SnykAnalyticsService(anonymousId, Segment.Analytics.Client, endpoint);
            Instance.AnalyticsEnabled = enabled;
        }

        public void LogAnalysisReadyEvent(AnalysisType analysisTypeParam, AnalyticsAnalysisResult analysisResultParam)
        {
            if (Disabled)
            {
                return;
            }

            var analysisResult = analysisResultParam.ToAnalysisIsReadyEnum();
            var analysisType = analysisTypeParam.ToAnalysisIsReadyEnum();

            Itly.AnalysisIsReady(this.userId, analysisType, AnalysisIsReady.Ide.VisualStudio, analysisResult);
        }

        public void LogWelcomeIsViewedEvent()
        {
            if (Disabled)
            {
                return;
            }

            Itly.WelcomeIsViewed(this.userId);
        }

        public void LogAnalysisIsTriggeredEvent(IList<AnalysisType> selectedProducts)
        {
            if (Disabled)
            {
                return;
            }

            var selectedProductsAsStrings = selectedProducts
                .Select(analysisType => analysisType.ToAmplitudeString())
                .ToArray();

            Itly.AnalysisIsTriggered(this.userId, selectedProductsAsStrings, AnalysisIsTriggered.Ide.VisualStudio, true);
        }

        public void LogIssueIsViewedEvent(string id, ScanResultIssueType issueTypeParam, string severityParam)
        {
            if (Disabled)
            {
                return;
            }

            var issueType = issueTypeParam.ToIssueInTreeIsClickedEnum();

            IssueInTreeIsClicked.Severity severity;
            switch (severityParam)
            {
                case "High": 
                    severity = IssueInTreeIsClicked.Severity.High;
                    break;
                case "Medium":
                    severity = IssueInTreeIsClicked.Severity.Medium;
                    break;
                case "Low":
                    severity = IssueInTreeIsClicked.Severity.Low;
                    break;
                case "Critical":
                    severity = IssueInTreeIsClicked.Severity.Critical;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(severityParam));
            }

            Itly.IssueInTreeIsClicked(this.userId, IssueInTreeIsClicked.Ide.VisualStudio, id, issueType, severity);
        }

        public async Task ObtainUserAsync(string apiToken, string vsVersion)
        {
            this.vsVersion = vsVersion;
            await ObtainUserAsync(apiToken);
        }

        public async Task ObtainUserAsync(string apiToken)
        {
            if (this.segmentClient == null || string.IsNullOrEmpty(apiToken) || !string.IsNullOrEmpty(this.userId) || this.Disabled)
            {
                this.userId = null;
                return;
            }

            var user = await GetSnykUserAsync(apiToken);

            if (string.IsNullOrEmpty(user.Id))
            {
                Logger.Information("Can't obtain user because userId is empty");
                return;
            }

            this.userId = user.Id;

            var context = new Segment.Model.Context().Add("app", new Dict()
            {
                {"name", "Visual Studio"},
                {"version", this.vsVersion},
            });
            var options = new Segment.Model.Options()
                .SetAnonymousId(this.anonymousId)
                .SetContext(context);

            this.segmentClient.Identify(this.userId, null, options);
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
            Logger.Information("Shutting down analytics service...");
            Itly.Dispose();
            Logger.Information("Analytics service shut down complete");
        }

        private async Task<SnykUser> GetSnykUserAsync(string token)
        {
            using (var webClient = new SnykWebClient())
            {
                webClient.Headers.Add("Authorization", $"token {token}");
                webClient.Headers.Add("Accept", "application/json");
                webClient.Headers.Add("Content-Type", "application/json");

                var userInfoJson = await webClient.DownloadStringTaskAsync(this.userMeEndpoint);

                return Json.Deserialize<SnykUser>(userInfoJson);
            }
        }
    }
}
