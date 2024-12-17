using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Service;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.Settings
{
    public class SnykOptionsManager : ISnykOptionsManager
    {
        private readonly ISnykServiceProvider serviceProvider;
        private readonly IUserStorageSettingsService userSettingsStorage;

        public SnykOptionsManager(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.userSettingsStorage = serviceProvider.UserStorageSettingsService;
        }

        public IPersistableOptions Load()
        {
            return new SnykOptions
            {
                DeviceId = userSettingsStorage.DeviceId,
                TrustedFolders = userSettingsStorage.TrustedFolders,
                AnalyticsPluginInstalledSent = userSettingsStorage.AnalyticsPluginInstalledSent,
                AutoScan = userSettingsStorage.AutoScan,
                IgnoreUnknownCA = userSettingsStorage.IgnoreUnknownCa,

                BinariesAutoUpdate = userSettingsStorage.BinariesAutoUpdate,
                CliCustomPath = userSettingsStorage.CliCustomPath,
                CliDownloadUrl = userSettingsStorage.CliDownloadUrl,
                CliReleaseChannel = userSettingsStorage.CliReleaseChannel,
                CurrentCliVersion = userSettingsStorage.CurrentCliVersion,

                AuthenticationMethod = userSettingsStorage.AuthenticationMethod,
                ApiToken = new AuthenticationToken(userSettingsStorage.AuthenticationMethod, userSettingsStorage.Token),
                CustomEndpoint = userSettingsStorage.CustomEndpoint,
                Organization = userSettingsStorage.Organization,

                FolderConfigs = userSettingsStorage.FolderConfigs,
                EnableDeltaFindings = userSettingsStorage.EnableDeltaFindings,

                OpenIssuesEnabled = userSettingsStorage.OpenIssuesEnabled,
                IgnoredIssuesEnabled = userSettingsStorage.IgnoredIssuesEnabled,

                IacEnabled = userSettingsStorage.IacEnabled,
                SnykCodeQualityEnabled = userSettingsStorage.SnykCodeQualityEnabled,
                SnykCodeSecurityEnabled = userSettingsStorage.SnykCodeSecurityEnabled,
                OssEnabled = userSettingsStorage.OssEnabled,
            };
        }

        public void Save(IPersistableOptions options)
        {
            userSettingsStorage.DeviceId = options.DeviceId;
            userSettingsStorage.TrustedFolders = options.TrustedFolders;
            userSettingsStorage.AnalyticsPluginInstalledSent = options.AnalyticsPluginInstalledSent;
            userSettingsStorage.AutoScan = options.AutoScan;
            userSettingsStorage.IgnoreUnknownCa = options.IgnoreUnknownCA;

            userSettingsStorage.BinariesAutoUpdate = options.BinariesAutoUpdate;
            userSettingsStorage.CliCustomPath = options.CliCustomPath;
            userSettingsStorage.CliDownloadUrl = options.CliDownloadUrl;
            userSettingsStorage.CliReleaseChannel = options.CliReleaseChannel;
            userSettingsStorage.CurrentCliVersion = options.CurrentCliVersion;

            userSettingsStorage.AuthenticationMethod = options.AuthenticationMethod;
            userSettingsStorage.Token = options.ApiToken.ToString();

            userSettingsStorage.CustomEndpoint = options.CustomEndpoint;
            userSettingsStorage.Organization = options.Organization;

            userSettingsStorage.FolderConfigs = options.FolderConfigs;
            userSettingsStorage.EnableDeltaFindings = options.EnableDeltaFindings;

            userSettingsStorage.OpenIssuesEnabled = options.OpenIssuesEnabled;
            userSettingsStorage.IgnoredIssuesEnabled = options.IgnoredIssuesEnabled;

            userSettingsStorage.IacEnabled = options.IacEnabled;
            userSettingsStorage.SnykCodeQualityEnabled = options.SnykCodeQualityEnabled;
            userSettingsStorage.SnykCodeSecurityEnabled = options.SnykCodeSecurityEnabled;
            userSettingsStorage.OssEnabled = options.OssEnabled;

            this.userSettingsStorage.SaveSettings();
        }

        /// <summary>
        /// Gets a value indicating whether additional options.
        /// Get this data using <see cref="SnykUserStorageSettingsService"/>.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<string> GetAdditionalOptionsAsync() => await this.userSettingsStorage.GetAdditionalOptionsAsync();

        /// <summary>
        /// Gets a value indicating whether is scan all projects enabled via <see cref="SnykUserStorageSettingsService"/>.
        /// Get this data using <see cref="SnykUserStorageSettingsService"/>.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<bool> IsScanAllProjectsAsync() => await this.userSettingsStorage.GetIsAllProjectsEnabledAsync();
    }
}
