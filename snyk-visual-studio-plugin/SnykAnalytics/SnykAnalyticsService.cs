using Newtonsoft.Json;
using Segment;
using Segment.Model;
using Snyk.VisualStudio.Extension.Settings;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.SnykAnalytics
{
    public class SnykAnalyticsService
    {
        private const string SnykUserMeUrl = "https://snyk.io/api/user/me/";

        public const string OpenSourceAnalysisReady = "Snyk Open Source Analysis Ready";
        public const string UserLandedOnTheWelcomePage = "User Landed On The Welcome Page";
        public const string UserSeesAnIssue = "User Sees An Issue";
        public const string UserTriggersAnAnalysis = "User Triggers An Analysis";

        private Client analyticsClient;

        private string anonymousUserId = Guid.NewGuid().ToString();

        private bool analyticsInitialized = true;

        public string UserId { get; set; }

        public bool AnalyticsEnabled { get; set; }

        public SnykActivityLogger Logger { get; set; }

        public SnykAnalyticsService()
        {
            AnalyticsEnabled = true;
        }

        public void Initialize()
        {
            try
            {
                string extensionPath = SnykExtension.GetExtensionDirectoryPath();

                string appsettingsPath = Path.Combine(extensionPath, "appsettings.json");

                SnykAppSettings appSettings = JsonConvert
                    .DeserializeObject<SnykAppSettings>(File.ReadAllText(appsettingsPath, Encoding.UTF8));

                string writeKey = appSettings.SegmentAnalyticsWriteKey;

                if (String.IsNullOrEmpty(writeKey))
                {
                    analyticsInitialized = false;

                    Logger.LogInformation("Segment analytics collection is disabled because write key is empty!");
                } else
                {                    
                    Analytics.Initialize(appSettings.SegmentAnalyticsWriteKey, new Config()
                            .SetAsync(true)
                            .SetTimeout(TimeSpan.FromSeconds(10))
                            .SetMaxQueueSize(5));

                    analyticsClient = Analytics.Client;

                    Identify();
                }                
            }
            catch (Exception exception)
            {
                analyticsInitialized = false;

                Logger.LogError(exception.Message);
            }
        }

        public void ObtainUser(string token) 
            => Execute(() =>
            {
                if (Disabled) return;

                if (string.IsNullOrEmpty(token)) return;

                if (!string.IsNullOrEmpty(UserId)) return;

                SnykUser user = GetSnykUser(token);

                if (user != null) Alias(user.Id);
            });

        public void Dispose()
        {
            Logger.LogInformation("Flush events in the message queue and stop this segment instance.");

            analyticsClient?.Flush();
            analyticsClient?.Dispose();
        }        

        public void LogOpenSourceAnalysisReadyEvent(int highSeverityCount, int mediumSeverityCount, int lowSeverityCount) 
            => LogEvent(SnykAnalyticsService.OpenSourceAnalysisReady, new Properties()
            {
                { "highSeverityIssuesCount", highSeverityCount },
                { "mediumSeverityIssuesCount", mediumSeverityCount },
                { "lowSeverityIssuesCount", lowSeverityCount }
            });

        public void LogUserLandedOnTheWelcomePageEvent() 
            => LogEvent(SnykAnalyticsService.UserLandedOnTheWelcomePage, new Properties());

        public void LogUserTriggersAnAnalysisEvent() 
            => LogEvent(SnykAnalyticsService.UserTriggersAnAnalysis, new Properties()
            {
                { "selectedProducts", "Snyk Open Source" }
            });

        public void LogUserSeesAnIssueEvent(string id, string severity) 
            => LogEvent(SnykAnalyticsService.UserSeesAnIssue, new Properties()
            {
                {
                    "issueDetails", new Properties()
                    {
                        { "id", id},
                        { "severity", severity }
                    }
                }
            });

        private void Identify()
        {
            if (Disabled) return;

            analyticsClient?.Identify(anonymousUserId, new Traits());
        }

        private void Alias(string userId)
            => Execute(() =>
            {
                if (Disabled) return;

                if (String.IsNullOrEmpty(userId))
                {
                    Logger.LogInformation("Alias event cannot be executed because userId is empty");

                    return;
                }

                UserId = userId;

                analyticsClient?.Alias(anonymousUserId, userId);
            });

        private void LogEvent(string eventName, Properties properties)
            => Execute(() =>
            {
                if (Disabled) return;

                string userId = String.IsNullOrEmpty(UserId) ? anonymousUserId : UserId;

                Logger.LogInformation($"Analytics client track event {eventName}.");

                analyticsClient?.Track(userId, eventName, properties);
            });

        private SnykUser GetSnykUser(string token)
        {
            using (var webClient = new SnykWebClient())
            {
                webClient.Headers.Add("Authorization", $"token {token}");
                webClient.Headers.Add("Accept", "application/json");
                webClient.Headers.Add("Content-Type", "application/json");

                string userInfoJson = webClient.DownloadString(SnykUserMeUrl);

                return JsonConvert.DeserializeObject<SnykUser>(userInfoJson);
            }
        }

        private bool Enabled => AnalyticsEnabled && analyticsInitialized;

        private bool Disabled => !Enabled;

        private void Execute(Action method)
        {
            new Task(() =>
            {
                try
                {
                    method();
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception.Message);
                }
            }).Start();
        }

        private SnykUser GetSnykUser(string token)
        {
            using (var webClient = new SnykWebClient())
            {
                webClient.Headers.Add("Authorization", $"token {token}");
                webClient.Headers.Add("Accept", "application/json");
                webClient.Headers.Add("Content-Type", "application/json");

                string userInfoJson = webClient.DownloadString(SnykUserMeUrl);

                return JsonConvert.DeserializeObject<SnykUser>(userInfoJson);
            }
        }

        private bool Enabled => AnalyticsEnabled && analyticsInitialized;

        private bool Disabled => !Enabled;

        private void Execute(Action method)
        {
            new Task(() =>
            {
                try
                {
                    method();
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception.Message);
                }
            }).Start();
        }
    }

    public class SnykUser
    {
        public string Id { get; set; }
    }

    public class SnykUser
    {
        public string Id { get; set; }
    }
}
