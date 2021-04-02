using Newtonsoft.Json;
using Segment;
using Segment.Model;
using Snyk.VisualStudio.Extension.Settings;
using System;
using System.IO;
using System.Text;

namespace Snyk.VisualStudio.Extension.SnykAnalytics
{
    public class SnykAnalyticsService
    {
        private const string SnykUserMeUrl = "https://snyk.io/api/user/me/";

        public const string OpenSourceAnalysisReady = "Snyk Open Source Analysis Ready";
        public const string UserLandedOnTheWelcomePage = "User Landed On The Welcome Page";
        public const string UserSeesAnIssue = "User Sees An Issue";
        public const string UserTriggersAnAnalysis = "User Triggers An Analysis";
        public const string UserTriggersItsFirstAnalysis = "User Triggers Its First Analysis";        

        private Client analyticsClient;
        private string anonymousUserId = Guid.NewGuid().ToString();

        public string UserId { get; set; }

        public bool AnalyticsEnabled { get; set; }

        public SnykActivityLogger Logger { get; set; }

        public SnykAnalyticsService()
        {
            AnalyticsEnabled = true;
        }

        public void Initialize()
        {
            Logger.LogInformation("Enter Initialize.");            

            try
            {                
                SnykAppSettings appSettings = JsonConvert
                    .DeserializeObject<SnykAppSettings>(File.ReadAllText("appsettings.json", Encoding.UTF8));

                string writeKey = appSettings.SegmentAnalyticsWriteKey;

                if (String.IsNullOrEmpty(writeKey))
                {
                    AnalyticsEnabled = false;

                    Logger.LogInformation("Segment analytics collection is disabled because write key is empty!");
                } else
                {                    
                    Analytics.Initialize(appSettings.SegmentAnalyticsWriteKey, new Config()
                            .SetAsync(true)
                            .SetTimeout(TimeSpan.FromSeconds(10))
                            .SetMaxQueueSize(5));

                    analyticsClient = Analytics.Client;
                }                
            }
            catch (Exception exception)
            {
                AnalyticsEnabled = false;

                Logger.LogError(exception.Message);
            }

            Logger.LogInformation("Leave Initialize.");
        }

        public void ObtainUser(string token)
        {
            Logger.LogInformation("Enter ObtainUser");

            if (!AnalyticsEnabled) return;

            if (string.IsNullOrEmpty(token)) return;

            SnykUser user = GetSnykUser(token);

            if (user != null) Alias(user.Id);

            Logger.LogInformation("Leave ObtainUser");
        }

        public void Dispose()
        {
            Logger.LogInformation("Enter Dispose.");

            Logger.LogInformation("Flush events in the message queue and stop this segment instance.");

            analyticsClient?.Flush();
            analyticsClient?.Dispose();

            Logger.LogInformation("Leave Dispose.");
        }

        public void Identify()
        {
            Logger.LogInformation("Enter Identify.");

            if (!AnalyticsEnabled) return;

            try
            {
                analyticsClient?.Identify(anonymousUserId, new Traits());
            } 
            catch(Exception exception)
            {
                Logger.LogError(exception.Message);
            }
            

            Logger.LogInformation("Leave Identify.");
        }

        public void Alias(string userId)
        {
            Logger.LogInformation("Enter Alias.");

            if (!AnalyticsEnabled) return;

            if (String.IsNullOrEmpty(userId))
            {
                Logger.LogInformation("Alias event cannot be executed because userId is empty");

                return;
            }

            UserId = userId;

            try
            {
                analyticsClient?.Alias(anonymousUserId, userId);
            }
            catch (Exception exception)
            {
                UserId = "";

                Logger.LogError(exception.Message);
            }
            
            Logger.LogInformation("Leave Alias.");
        }

        public void LogEvent(string eventName, Properties properties)
        {
            Logger.LogInformation("Enter LogEvent.");

            if (!AnalyticsEnabled) return;

            try
            {
                string userId = String.IsNullOrEmpty(UserId) ? anonymousUserId : UserId;

                Logger.LogInformation($"Analytics client track event {eventName}.");

                analyticsClient?.Track(userId, eventName, properties);
            }
            catch (Exception exception)
            {                
                Logger.LogError(exception.Message);
            }

            Logger.LogInformation("Leave LogEvent.");
        }

        public SnykUser GetSnykUser(string token)
        {
            Logger.LogInformation("Enter GetSnykUser.");

            using (var webClient = new SnykWebClient())
            {
                webClient.Headers.Add("Authorization", $"token {token}");
                webClient.Headers.Add("Accept", "application/json");
                webClient.Headers.Add("Content-Type", "application/json");

                string userInfoJson = webClient.DownloadString(SnykUserMeUrl);

                Logger.LogInformation("Leave GetSnykUser and convert to SnykUser.");

                return JsonConvert.DeserializeObject<SnykUser>(userInfoJson);
            }
        }
    }

    public class SnykUser
    {
        public string Id { get; set; }
    }
}
