namespace Snyk.VisualStudio.Extension.Shared.SnykAnalytics
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Segment;
    using Segment.Model;
    using Serilog;
    using Snyk.Analytics;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.CLI;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using Snyk.VisualStudio.Extension.Shared.Settings;

    /// <summary>
    /// Analytics service.
    /// </summary>
    public class SnykAnalyticsService : ISnykAnalyticsService
    {
        /// <summary>
        /// Analysis Ready message.
        /// </summary>
        public const string AnalysisIsReady = "Analysis Is Ready";

        /// <summary>
        /// The Welcome Page is viewed event.
        /// </summary>
        public const string WelcomePageIsViewed = "Welcome Is Viewed";

        /// <summary>
        /// User Sees An Issue message.
        /// </summary>
        public const string IssueIsViewed = "Issue Is Viewed";

        /// <summary>
        /// User Triggers An Analysis.
        /// </summary>
        public const string AnalysisIsTriggered = "Analysis Is Triggered";

        private const string VisualStudioIde = "Visual Studio";

        /// <summary>
        /// Link to snyk.io user/me.
        /// </summary>
        private const string SnykUserMeUrl = "https://snyk.io/api/user/me/";

        private static readonly ILogger Logger = LogManager.ForContext<SnykAnalyticsService>();

        private static SnykAnalyticsService instance;

        private Client analyticsClient;

        private string anonymousUserId;

        private bool analyticsInitialized = true;

        private string userIdAsHash;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykAnalyticsService"/> class.
        /// </summary>
        /// <param name="anonymousUserId">Analytics anonymous user id.</param>
        private SnykAnalyticsService(string anonymousUserId)
        {
            this.AnalyticsEnabled = true;

            this.anonymousUserId = anonymousUserId;
        }

        /// <summary>
        /// Gets <see cref="SnykAnalyticsService"/> singleton instance.
        /// </summary>
        public static SnykAnalyticsService Instance => instance;

        /// <summary>
        /// Gets or sets a value indicating whether user id.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets a value indicating whether user id hash string.
        /// </summary>
        public string UserIdAsHash
        {
            get
            {
                if (string.IsNullOrEmpty(this.userIdAsHash) && !string.IsNullOrEmpty(this.UserId))
                {
                    this.userIdAsHash = Sha256.ComputeHash(this.UserId);
                }

                return this.userIdAsHash;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether is analytics enabled.
        /// </summary>
        public bool AnalyticsEnabled { get; set; }

        private bool Enabled => this.AnalyticsEnabled && this.analyticsInitialized;

        private bool Disabled => !this.Enabled;

        /// <summary>
        /// Initialize Analytics service intance.
        /// </summary>
        /// <param name="options">Snyk options.</param>
        public static void Initialize(ISnykOptions options)
        {
            try
            {
                SnykAppSettings appSettings = SnykExtension.AppSettings;

                if (string.IsNullOrEmpty(options.AnonymousId))
                {
                    options.AnonymousId = System.Guid.NewGuid().ToString();
                }

                instance = new SnykAnalyticsService(options.AnonymousId);

                string writeKey = appSettings?.SegmentAnalyticsWriteKey;

                if (string.IsNullOrEmpty(writeKey))
                {
                    instance.analyticsInitialized = false;

                    Logger.Information("Segment analytics collection is disabled because write key is empty!");
                }
                else
                {
                    Analytics.Initialize(writeKey, new Config()
                            .SetAsync(true)
                            .SetTimeout(TimeSpan.FromSeconds(10))
                            .SetMaxQueueSize(5));

                    instance.analyticsClient = Analytics.Client;

                    instance.Identify();
                }
            }
            catch (Exception e)
            {
                instance.analyticsInitialized = false;

                Logger.Error(e, "Error on initialize analytics service");
            }
        }

        public void ObtainUser(ISnykServiceProvider serviceProvider) => this.ObtainUser(serviceProvider, () => { });

        /// <summary>
        /// Obtain user by user token.
        /// </summary>
        /// <param name="serviceProvider">Service provider to get API token.</param>
        /// <param name="callback">Callback function for execute code after obtain user.</param>
        public void ObtainUser(ISnykServiceProvider serviceProvider, Action callback)
        {
            try
            {
                this.ObtainUser(serviceProvider.GetApiToken(), callback);
            }
            catch (InvalidTokenException e)
            {
                Logger.Error(e, "Error on obtain user");
            }
        }

        /// <summary>
        /// Flush and dispose analytics.
        /// </summary>
        public void Dispose()
        {
            Logger.Information("Flush events in the message queue and stop this segment instance.");

            this.analyticsClient?.Flush();
            this.analyticsClient?.Dispose();
        }

        /// <summary>
        /// Log Analysis Is Ready event.
        /// </summary>
        /// <param name="analysisType">Type of analysis (Oss, SnykCode Security or quality).</param>
        /// <param name="analysisResult">Analysis result (success or error).</param>
        public void LogAnalysisReadyEvent(string analysisType, string analysisResult)
            => this.LogEvent(SnykAnalyticsService.AnalysisIsReady, new Properties()
            {
                { "ide", VisualStudioIde },
                { "analysisType", analysisType },
                { "result", analysisResult },
            });

        /// <summary>
        /// Log UserLandedOnTheWelcomePageEvent.
        /// </summary>
        public void LogWelcomeIsViewedEvent()
            => this.LogEvent(SnykAnalyticsService.WelcomePageIsViewed, new Properties() { { "ide", VisualStudioIde }, });

        /// <summary>
        /// Log UserTriggersAnAnalysisEvent.
        /// </summary>
        /// <param name="selectedProducts">Selected products (OSS, SnykCode Quality and Security).</param>
        public void LogAnalysisIsTriggeredEvent(IList<string> selectedProducts)
            => this.LogEvent(SnykAnalyticsService.AnalysisIsTriggered, new Properties()
            {
                { "ide", VisualStudioIde },
                { "selectedProducts", selectedProducts },
                { "triggeredByUser", true },
            });

        /// <summary>
        /// Log Issue Is Viewed Event.
        /// </summary>
        /// <param name="id">User id.</param>
        /// <param name="issueType">Type of issue (Oss, SnykCode Security or Queality.</param>
        /// <param name="severity">Severity name.</param>
        public void LogIssueIsViewedEvent(string id, string issueType, string severity)
            => this.LogEvent(SnykAnalyticsService.IssueIsViewed, new Properties()
            {
                { "ide", VisualStudioIde },
                { "issueId", id },
                { "issueType", issueType },
                { "severity", severity },
            });

        private void Identify()
        {
            if (this.Disabled)
            {
                return;
            }

            this.analyticsClient?.Identify(this.anonymousUserId, new Traits());
        }

        private void Alias(string userId, Action callback)
            => this.Execute(
                () =>
                {
                    if (this.Disabled)
                    {
                        return;
                    }

                    if (string.IsNullOrEmpty(userId))
                    {
                        Logger.Information("Alias event cannot be executed because userId is empty");

                        return;
                    }

                    this.UserId = userId;

                    callback();

                    this.analyticsClient?.Alias(this.anonymousUserId, userId);
                });

        private void LogEvent(string eventName, Properties properties)
            => this.Execute(
                () =>
                {
                    if (this.Disabled)
                    {
                        return;
                    }

                    string userId = string.IsNullOrEmpty(this.UserId) ? this.anonymousUserId : this.UserId;

                    Logger.Information("Analytics client track event {EventName}.", eventName);

                    this.analyticsClient?.Track(userId, eventName, properties);
                });

        private SnykUser GetSnykUser(string token)
        {
            using (var webClient = new SnykWebClient())
            {
                webClient.Headers.Add("Authorization", $"token {token}");
                webClient.Headers.Add("Accept", "application/json");
                webClient.Headers.Add("Content-Type", "application/json");

                string userInfoJson = webClient.DownloadString(SnykUserMeUrl);

                return Json.Deserialize<SnykUser>(userInfoJson);
            }
        }

        private void ObtainUser(string token, Action callback)
            => this.Execute(
                () =>
                {
                    if (this.Disabled)
                    {
                        return;
                    }

                    if (string.IsNullOrEmpty(token))
                    {
                        return;
                    }

                    if (!string.IsNullOrEmpty(this.UserId))
                    {
                        return;
                    }

                    var user = this.GetSnykUser(token);

                    if (user != null)
                    {
                        this.Alias(user.Id, callback);
                    }
                });

        private void Execute(Action method)
        {
            new Task(
                () =>
                {
                    try
                    {
                        method();
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(exception.Message);
                    }
                }).Start();
        }
    }
}
