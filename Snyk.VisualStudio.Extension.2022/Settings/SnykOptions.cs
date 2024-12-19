using System;
using System.Collections.Generic;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    public class SnykOptions : ISnykOptions
    {
        public string Application { get; set; }
        public string ApplicationVersion { get; set; }
        public string IntegrationName { get; } = SnykExtension.IntegrationName;
        public string IntegrationVersion { get; } = SnykExtension.Version;
        public string IntegrationEnvironment { get; set; }
        public string IntegrationEnvironmentVersion { get; set; }
        public bool ConsistentIgnoresEnabled { get; set; }
        public string DeviceId { get; set; }
        public bool AutoScan { get; set; }
        public bool OpenIssuesEnabled { get; set; }
        public bool IgnoredIssuesEnabled { get; set; }
        public AuthenticationToken ApiToken { get; set; }
        public AuthenticationType AuthenticationMethod { get; set; }
        public string CustomEndpoint { get; set; }
        public string Organization { get; set; }
        public bool IgnoreUnknownCA { get; set; }
        public bool OssEnabled { get; set; }
        public bool IacEnabled { get; set; }
        public bool SnykCodeSecurityEnabled { get; set; }
        public bool SnykCodeQualityEnabled { get; set; }
        public bool BinariesAutoUpdate { get; set; }
        public string CliCustomPath { get; set; }
        public string CliReleaseChannel { get; set; }
        public string CliDownloadUrl { get; set; }
        public ISet<string> TrustedFolders { get; set; }
        public bool EnableDeltaFindings { get; set; }
        public List<FolderConfig> FolderConfigs { get; set; }
        public string CurrentCliVersion { get; set; }
        public bool AnalyticsPluginInstalledSent { get; set; }
        public string SnykCodeSettingsUrl => $"{this.GetBaseAppUrl()}/manage/snyk-code";
        public event EventHandler<SnykSettingsChangedEventArgs> SettingsChanged;

        public void InvokeSettingsChangedEvent()
        {
            this.SettingsChanged?.Invoke(this, new SnykSettingsChangedEventArgs());
        }

        public string GetCustomApiEndpoint()
        {
            return string.IsNullOrEmpty(CustomEndpoint) ? ApiEndpointResolver.DefaultApiEndpoint : ApiEndpointResolver.TranslateOldApiToNewApiEndpoint(CustomEndpoint);
        }
        public string GetBaseAppUrl()
        {
            if (string.IsNullOrEmpty(CustomEndpoint))
                return ApiEndpointResolver.DefaultAppEndpoint;

            var result = ApiEndpointResolver.GetCustomEndpointUrlFromSnykApi(GetCustomApiEndpoint(), "app");

            return string.IsNullOrEmpty(result) ? ApiEndpointResolver.DefaultAppEndpoint : result;
        }
    }
}
