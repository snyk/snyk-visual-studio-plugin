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
        private string userId;
        private string vsVersion;
        private string userIdAsHash;
        private readonly Client segmentClient;

        private static readonly ILogger Logger = LogManager.ForContext<SnykAnalyticsService>();

        private SnykAnalyticsService(string anonymousId, Client segmentClient)
        {
            this.anonymousId = anonymousId;
            this.segmentClient = segmentClient;
        }

        public static SnykAnalyticsService Instance { get; private set; }

        public bool AnalyticsEnabled { get; set; }

        private bool Disabled => !AnalyticsEnabled || this.segmentClient == null;

        public static void Initialize(string anonymousId, string writeKey)
        {
            if (string.IsNullOrEmpty(anonymousId))
            {
                anonymousId = Guid.NewGuid().ToString();
            }

            if (string.IsNullOrEmpty(writeKey))
            {
                Instance = new SnykAnalyticsService(anonymousId, null);
                Instance.AnalyticsEnabled = false;
                Logger.Information("Segment analytics collection is disabled because write key is empty!");
                return;
            }

            var segmentDestination = new SegmentCustomDestination(writeKey);
            Itly.Load(new Iteratively.Options(new DestinationsOptions(new CustomOptions(segmentDestination))));
            
            Instance = new SnykAnalyticsService(anonymousId, Segment.Analytics.Client);
            Instance.AnalyticsEnabled = true;
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

        public void LogIssueIsViewedEvent(string id, string issueTypeParam, string severityParam)
        {
            if (Disabled)
            {
                return;
            }

            IssueInTreeIsClicked.IssueType issueType;
            switch (issueTypeParam)
            {
                case "Open Source Vulnerability":
                    issueType = IssueInTreeIsClicked.IssueType.OpenSourceVulnerability;
                    break;
                case "Licence Issue":
                    issueType = IssueInTreeIsClicked.IssueType.LicenceIssue;
                    break;
                case "Code Quality Issue":
                    issueType = IssueInTreeIsClicked.IssueType.CodeQualityIssue;
                    break;
                case "Code Security Vulnerability":
                    issueType = IssueInTreeIsClicked.IssueType.CodeSecurityVulnerability;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(issueTypeParam));
            }

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
            if (string.IsNullOrEmpty(apiToken) || !string.IsNullOrEmpty(this.userId) || this.Disabled)
            {
                this.userId = null;
                return;
            }

            var user = await SnykUser.GetUserAsync(apiToken);

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
            this.segmentClient?.Dispose();
        }
    }
}
