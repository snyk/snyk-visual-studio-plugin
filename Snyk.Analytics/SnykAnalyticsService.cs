namespace Snyk.Analytics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Iteratively;
    using Segment;
    using Segment.Model;
    using Snyk.Common;
    using Snyk.Common.Authentication;
    using Snyk.Common.Service;
    using Guid = System.Guid;
    using ILogger = Serilog.ILogger;

    public class SnykAnalyticsService : ISnykAnalyticsService
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykAnalyticsService>();
        private static bool ItlyLoaded;
        private static string VsVersion;

        private readonly string anonymousId;
        private readonly Client segmentClient;
        private string userId;
        private string userIdAsHash;
        private bool analyticsEnabledOption;
        private readonly ISnykApiService snykApiService;

        private SnykAnalyticsService(string anonymousId, Client segmentClient, ISnykApiService apiService)
        {
            this.anonymousId = anonymousId;
            this.segmentClient = segmentClient;
            this.snykApiService = apiService;
        }

        public static SnykAnalyticsService Instance { get; private set; }

        public bool AnalyticsEnabledOption
        {
            get => this.analyticsEnabledOption;
            set
            {
                if (this.analyticsEnabledOption == value)
                {
                    return;
                }

                this.analyticsEnabledOption = value;
                Logger.Information(value ? "Analytics set to enabled" : "Analytics set to disabled");
            }
        }

        private bool Disabled => !this.AnalyticsEnabledOption || this.segmentClient == null || !ItlyLoaded || string.IsNullOrEmpty(this.userId);

        public static void Initialize(string anonymousId, string writeKey, bool enabled, ISnykApiService apiService)
        {
            Logger.Information("Initializing analytics service instance");

            // Verify write key
            if (string.IsNullOrEmpty(writeKey))
            {
                // Write key cannot change during execution, so leave the service in invalid state.
                // "Disabled" should be true because the segment client is missing
                Instance = new SnykAnalyticsService(anonymousId, null, apiService);
                Logger.Information("Analytics disabled because of empty write key");
                return;
            }

            // Initialize Segment
            var segmentDestination = new SegmentCustomDestination(writeKey);
            var segmentClient = Segment.Analytics.Client;

            // Verify or create new anonymous ID
            if (string.IsNullOrEmpty(anonymousId))
            {
                Logger.Information("Missing user anonymous ID, generating new one");
                anonymousId = Guid.NewGuid().ToString();
            }

            // Load Itly
            if(!ItlyLoaded)
            {
                Logger.Information("Loading Iteratively");
                Itly.Load(new Iteratively.Options(new DestinationsOptions(new CustomOptions(segmentDestination))));
                ItlyLoaded = true;
            }
            else
            {
                Logger.Information("Iteratively already loaded - skipping Load call");
            }

            // Create analytics service instance
            Instance = new SnykAnalyticsService(anonymousId, segmentClient, apiService);
            Instance.AnalyticsEnabledOption = enabled;
            Logger.Information("Analytics service instance initialized");
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
            switch (severityParam.ToLowerInvariant())
            {
                case "high": 
                    severity = IssueInTreeIsClicked.Severity.High;
                    break;
                case "medium":
                    severity = IssueInTreeIsClicked.Severity.Medium;
                    break;
                case "low":
                    severity = IssueInTreeIsClicked.Severity.Low;
                    break;
                case "critical":
                    severity = IssueInTreeIsClicked.Severity.Critical;
                    break;
                default:
                    Logger.Error("Failed to track \"Issue in tree is clicked\" event due to unknown severity: {Severity}", severityParam);
                    return;
            }

            Itly.IssueInTreeIsClicked(this.userId, IssueInTreeIsClicked.Ide.VisualStudio, id, issueType, severity);
        }

        public async Task ObtainUserAsync(AuthenticationToken apiToken, string vsVersion)
        {
            VsVersion = vsVersion;
            await ObtainUserAsync(apiToken);
        }

        public async Task ObtainUserAsync(AuthenticationToken apiToken)
        {
            Logger.Information("Identifying user");
            
            if (this.segmentClient == null)
            {
                Logger.Information("Analytics service not initialized, identification stopped");
                return;
            }

            if (!this.AnalyticsEnabledOption)
            {
                Logger.Information("Analytics service disabled, identification stopped");
                return;
            }

            if (!apiToken.IsValid())
            {
                Logger.Information("Analytics disabled - API token is missing");
                this.userId = null;
                return;
            }

            SnykUser user = null;
            try
            {
                user = await this.snykApiService.GetUserAsync();
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to obtain user ID");
            }

            if (user == null || string.IsNullOrEmpty(user.Id))
            {
                Logger.Information("Analytics disabled - Can't obtain user ID");
                this.userId = null;
                return;
            }
            
            // If user ID hasn't changed, do nothing
            if (this.userId == user.Id)
            {
                return;
            }

            this.userId = user.Id;

            Logger.Information("Identifying segment client. User-Id = {UserId}, Anonymous Id = {AnonymousId}, Vs Version: {VsVersion}",
                this.userId,
                this.anonymousId,
                VsVersion);

            var context = new Segment.Model.Context().Add("app", new Dict()
            {
                {"name", "Visual Studio"},
                {"version", VsVersion},
            });
            var options = new Segment.Model.Options()
                .SetAnonymousId(this.anonymousId)
                .SetContext(context);

            this.segmentClient.Identify(this.userId, null, options);

            Logger.Information("Analytics identification completed");
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
    }
}
