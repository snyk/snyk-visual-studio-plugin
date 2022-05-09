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

    public class SnykAnalyticsClient : ISnykAnalyticsService
    {
        private readonly string anonymousId;
        private string userId;
        private string userIdAsHash;
        private readonly Client segmentClient;

        private static readonly ILogger Logger = LogManager.ForContext<SnykAnalyticsClient>();

        private SnykAnalyticsClient(string anonymousId, Client segmentClient)
        {
            this.anonymousId = anonymousId;
            this.segmentClient = segmentClient ?? throw new InvalidOperationException("Segment client not initialized");
            this.segmentClient.Identify(this.anonymousId, new Traits());
        }

        public static SnykAnalyticsClient Instance { get; private set; }

        public bool AnalyticsEnabled { get; set; } = true;

        public static void Initialize(string anonymousId, string writeKey)
        {
            if (string.IsNullOrEmpty(anonymousId))
            {
                anonymousId = Guid.NewGuid().ToString();
            }

            Segment.Analytics.Initialize(writeKey, new Config()
                .SetAsync(true)
                .SetTimeout(TimeSpan.FromSeconds(10))
                .SetMaxQueueSize(5));

            var segmentDestination = new SegmentCustomDestination(writeKey);
            segmentDestination.Identify(anonymousId, new Iteratively.Properties());
            
            Itly.Load(new Iteratively.Options(new DestinationsOptions(new CustomOptions(segmentDestination))));
            
            Instance = new SnykAnalyticsClient(anonymousId, Segment.Analytics.Client);
        }

        public void LogAnalysisReadyEvent(AnalysisTypeEnum analysisTypeParam, string analysisResult)
        {
            AnalysisIsReady.Result result;
            switch (analysisResult)
            {
                case "Success":
                    result = AnalysisIsReady.Result.Success;
                    break;
                case "Error":
                    result = AnalysisIsReady.Result.Error;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(analysisResult), analysisResult, null);
            }

            AnalysisIsReady.AnalysisType analysisType;
            switch (analysisTypeParam)
            {
                case AnalysisTypeEnum.SnykOpenSource:
                    analysisType = AnalysisIsReady.AnalysisType.SnykOpenSource;
                    break;
                case AnalysisTypeEnum.SnykCodeSecurity:
                    analysisType = AnalysisIsReady.AnalysisType.SnykCodeSecurity;
                    break;
                case AnalysisTypeEnum.SnykCodeQuality:
                    analysisType = AnalysisIsReady.AnalysisType.SnykCodeQuality;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(analysisTypeParam), analysisTypeParam, null);
            }

            Itly.AnalysisIsReady(this.userId, analysisType, AnalysisIsReady.Ide.VisualStudio, result);
        }

        public void LogAnalysisReadyEvent(string analysisType, string analysisResult)
        {
            AnalysisIsReady.Result result;
            switch (analysisResult)
            {
                case "Success":
                    result = AnalysisIsReady.Result.Success;
                    break;
                case "Error":
                    result = AnalysisIsReady.Result.Error;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(analysisResult), analysisResult, null);
            }

            AnalysisIsReady.AnalysisType analysisTypeEnum;
            switch (analysisType)
            {
                case "Snyk Open Source":
                    analysisTypeEnum = AnalysisIsReady.AnalysisType.SnykOpenSource;
                    break;
                case "Snyk Code Security":
                    analysisTypeEnum = AnalysisIsReady.AnalysisType.SnykCodeSecurity;
                    break;
                case "Snyk Code Quality":
                    analysisTypeEnum = AnalysisIsReady.AnalysisType.SnykCodeQuality;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(analysisType), analysisType, null);
            }

            Itly.AnalysisIsReady(this.userId, analysisTypeEnum, AnalysisIsReady.Ide.VisualStudio, result);
        }

        public void LogWelcomeIsViewedEvent()
        {
            Itly.WelcomeIsViewed(this.userId);
        }

        public void LogAnalysisIsTriggeredEvent(IList<string> selectedProducts)
        {
            Itly.AnalysisIsTriggered(this.userId, selectedProducts.ToArray(), AnalysisIsTriggered.Ide.VisualStudio, true);
        }

        public void LogIssueIsViewedEvent(string id, string issueTypeParam, string severityParam)
        {
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

            this.segmentClient.Alias(this.anonymousId, this.userId);
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
