namespace Snyk.VisualStudio.Extension.SnykAnalytics
{
    using System;
    using System.Threading.Tasks;
    using Segment;
    using Segment.Model;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Settings;

    /// <summary>
    /// Analytics service.
    /// </summary>
    public class SnykAnalyticsService
    {
        /// <summary>
        /// Snyk Open Source Analysis Ready message.
        /// </summary>
        public const string OpenSourceAnalysisReady = "Snyk Open Source Analysis Ready";

        /// <summary>
        /// User Landed On The Welcome Page message.
        /// </summary>
        public const string UserLandedOnTheWelcomePage = "User Landed On The Welcome Page";

        /// <summary>
        /// User Sees An Issue message.
        /// </summary>
        public const string UserSeesAnIssue = "User Sees An Issue";

        /// <summary>
        /// User Triggers An Analysis message.
        /// </summary>
        public const string UserTriggersAnAnalysis = "User Triggers An Analysis";

        /// <summary>
        /// Link to snyk.io user/me.
        /// </summary>
        private const string SnykUserMeUrl = "https://snyk.io/api/user/me/";

        private Client analyticsClient;

        private string anonymousUserId = Guid.NewGuid().ToString();

        private bool analyticsInitialized = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykAnalyticsService"/> class.
        /// </summary>
        public SnykAnalyticsService() => this.AnalyticsEnabled = true;

        /// <summary>
        /// Gets or sets a value indicating whether user id.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is analytics enabled.
        /// </summary>
        public bool AnalyticsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether VS logger.
        /// </summary>
        public SnykActivityLogger Logger { get; set; }

        private bool Enabled => this.AnalyticsEnabled && this.analyticsInitialized;

        private bool Disabled => !this.Enabled;

        /// <summary>
        /// Initialize service.
        /// </summary>
        public void Initialize()
        {
            try
            {
                SnykAppSettings appSettings = SnykExtension.GetAppSettings();

                string writeKey = appSettings?.SegmentAnalyticsWriteKey;

                if (string.IsNullOrEmpty(writeKey))
                {
                    this.analyticsInitialized = false;

                    this.Logger.LogInformation("Segment analytics collection is disabled because write key is empty!");
                }
                else
                {
                    Analytics.Initialize(appSettings?.SegmentAnalyticsWriteKey, new Config()
                            .SetAsync(true)
                            .SetTimeout(TimeSpan.FromSeconds(10))
                            .SetMaxQueueSize(5));

                    this.analyticsClient = Analytics.Client;

                    this.Identify();
                }
            }
            catch (Exception exception)
            {
                this.analyticsInitialized = false;

                this.Logger.LogError(exception.Message);
            }
        }

        /// <summary>
        /// Obtain user by user token.
        /// </summary>
        /// <param name="token">API token.</param>
        public void ObtainUser(string token)
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

                    SnykUser user = this.GetSnykUser(token);

                    if (user != null)
                    {
                        this.Alias(user.Id);
                    }
                });

        /// <summary>
        /// Flush and dispose analytics.
        /// </summary>
        public void Dispose()
        {
            this.Logger.LogInformation("Flush events in the message queue and stop this segment instance.");

            this.analyticsClient?.Flush();
            this.analyticsClient?.Dispose();
        }

        /// <summary>
        /// Log OpenSourceAnalysisReadyEvent.
        /// </summary>
        /// <param name="criticalSeverityCount">Critical severity count.</param>
        /// <param name="highSeverityCount">High severity count.</param>
        /// <param name="mediumSeverityCount">Medium severity count.</param>
        /// <param name="lowSeverityCount">Low severity count.</param>
        public void LogOpenSourceAnalysisReadyEvent(int criticalSeverityCount, int highSeverityCount, int mediumSeverityCount, int lowSeverityCount)
            => this.LogEvent(SnykAnalyticsService.OpenSourceAnalysisReady, new Properties()
            {
                { "criticalSeverityIssuesCount", criticalSeverityCount },
                { "highSeverityIssuesCount", highSeverityCount },
                { "mediumSeverityIssuesCount", mediumSeverityCount },
                { "lowSeverityIssuesCount", lowSeverityCount },
            });

        /// <summary>
        /// Log UserLandedOnTheWelcomePageEvent.
        /// </summary>
        public void LogUserLandedOnTheWelcomePageEvent()
            => this.LogEvent(SnykAnalyticsService.UserLandedOnTheWelcomePage, new Properties());

        /// <summary>
        /// Log UserTriggersAnAnalysisEvent.
        /// </summary>
        public void LogUserTriggersAnAnalysisEvent()
            => this.LogEvent(SnykAnalyticsService.UserTriggersAnAnalysis, new Properties()
            {
                { "selectedProducts", "Snyk Open Source" },
            });

        /// <summary>
        /// Log UserSeesAnIssueEvent.
        /// </summary>
        /// <param name="id">User id.</param>
        /// <param name="severity">Severity name.</param>
        public void LogUserSeesAnIssueEvent(string id, string severity)
            => this.LogEvent(SnykAnalyticsService.UserSeesAnIssue, new Properties()
            {
                {
                    "issueDetails", new Properties()
                    {
                        { "id", id},
                        { "severity", severity },
                    }
                },
            });

        private void Identify()
        {
            if (this.Disabled)
            {
                return;
            }

            this.analyticsClient?.Identify(this.anonymousUserId, new Traits());
        }

        private void Alias(string userId)
            => this.Execute(
                () =>
                {
                    if (this.Disabled)
                    {
                        return;
                    }

                    if (string.IsNullOrEmpty(userId))
                    {
                        this.Logger.LogInformation("Alias event cannot be executed because userId is empty");

                        return;
                    }

                    this.UserId = userId;

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

                    this.Logger.LogInformation($"Analytics client track event {eventName}.");

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
                        this.Logger.LogError(exception.Message);
                    }
                }).Start();
        }
    }
}
