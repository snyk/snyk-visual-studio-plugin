using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    public class SnykOptions : ISnykOptions
    {
        private readonly ISnykServiceProvider serviceProvider;
        private readonly IUserStorageSettingsService userStorageSettingsService;
        private static readonly ILogger Logger = LogManager.ForContext<SnykOptions>();

        public SnykOptions(ISnykServiceProvider provider)
        {
            this.serviceProvider = provider;
            this.userStorageSettingsService = this.serviceProvider.UserStorageSettingsService;
        }

        public string Application { get; set; }
        public string ApplicationVersion { get; set; }
        public string IntegrationName { get; } = SnykExtension.IntegrationName;
        public string IntegrationVersion { get; } = SnykExtension.Version;
        public string IntegrationEnvironment { get; set; }
        public string IntegrationEnvironmentVersion { get; set; }

        public string DeviceId
        {
            get => this.userStorageSettingsService.DeviceId;
            set => this.userStorageSettingsService.DeviceId = value;
        }

        public bool AutoScan
        {
            get => this.userStorageSettingsService.AutoScan;
            set
            {
                if (this.userStorageSettingsService == null)
                    return;
                this.userStorageSettingsService.AutoScan = value;
            }
        }

        public bool ConsistentIgnoresEnabled { get; set; }
        public bool OpenIssuesEnabled
        {
            get => this.userStorageSettingsService.OpenIssuesEnabled;
            set
            {
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.OpenIssuesEnabled == value)
                    return;
                this.userStorageSettingsService.OpenIssuesEnabled = value;
            }
        }

        public bool IgnoredIssuesEnabled
        {
            get => this.userStorageSettingsService.IgnoredIssuesEnabled;
            set
            {
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.IgnoredIssuesEnabled == value)
                    return;
                this.userStorageSettingsService.IgnoredIssuesEnabled = value;
            }
        }

        public AuthenticationToken ApiToken
        {
            get => new AuthenticationToken(this.AuthenticationMethod, this.userStorageSettingsService.Token);
            set
            {
                var tokenAsString = value.ToString();
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.Token == tokenAsString)
                    return;
                this.userStorageSettingsService.Token = tokenAsString;
                userStorageSettingsService.SaveSettings();
                InvokeSettingsChangedEvent();
            }
        }

        public AuthenticationType AuthenticationMethod
        {
            get => this.userStorageSettingsService.AuthenticationMethod;
            set
            {
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.AuthenticationMethod == value)
                    return;
                this.userStorageSettingsService.AuthenticationMethod = value;
                ApiToken = AuthenticationToken.EmptyToken;
                userStorageSettingsService.SaveSettings();
                InvokeSettingsChangedEvent();
            }
        }
        public string CustomEndpoint
        {
            get => this.userStorageSettingsService.CustomEndpoint;
            set
            {
                if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
                {
                    Logger.Warning("Custom endpoint value is not a well-formed URI. Setting custom endpoint to empty string");
                    value = string.Empty;
                }

                var newApiEndpoint = ApiEndpointResolver.TranslateOldApiToNewApiEndpoint(value);
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.CustomEndpoint == newApiEndpoint)
                {
                    return;
                }

                this.userStorageSettingsService.CustomEndpoint = newApiEndpoint;
                ApiToken = AuthenticationToken.EmptyToken;
                userStorageSettingsService.SaveSettings();
                InvokeSettingsChangedEvent();
            }
        }
        public string SnykCodeSettingsUrl => $"{this.GetBaseAppUrl()}/manage/snyk-code";

        /// <summary>
        /// Gets or sets a value indicating whether organization.
        /// </summary>
        public string Organization
        {
            get => this.userStorageSettingsService.Organization;
            set
            {
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.Organization == value)
                {
                    return;
                }
                this.userStorageSettingsService.Organization = value;
                userStorageSettingsService.SaveSettings();
                InvokeSettingsChangedEvent();
            }
        }
        /// <summary>
        /// Gets or sets a value indicating whether ignore unknown CA.
        /// </summary>
        public bool IgnoreUnknownCA
        {
            get => this.userStorageSettingsService.IgnoreUnknownCa;
            set
            {
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.IgnoreUnknownCa == value)
                {
                    return;
                }
                this.userStorageSettingsService.IgnoreUnknownCa = value;
                userStorageSettingsService.SaveSettings();
                InvokeSettingsChangedEvent();
            }
        }
        /// <inheritdoc/>
        public bool OssEnabled
        {
            get => this.userStorageSettingsService.OssEnabled;
            set
            {
                if (this.userStorageSettingsService == null || userStorageSettingsService.OssEnabled == value)
                {
                    return;
                }

                this.userStorageSettingsService.OssEnabled = value;
            }
        }

        public bool IacEnabled
        {
            get => this.userStorageSettingsService.IacEnabled;
            set
            {
                if (this.userStorageSettingsService == null || userStorageSettingsService.IacEnabled == value)
                {
                    return;
                }

                this.userStorageSettingsService.IacEnabled = value;
            }
        }
        public bool SnykCodeSecurityEnabled
        {
            get => this.userStorageSettingsService.SnykCodeSecurityEnabled;
            set
            {
                if (this.userStorageSettingsService == null || userStorageSettingsService.SnykCodeSecurityEnabled == value)
                {
                    return;
                }

                this.userStorageSettingsService.SnykCodeSecurityEnabled = value;
            }
        }

        /// <inheritdoc/>
        public bool SnykCodeQualityEnabled
        {
            get => this.userStorageSettingsService.SnykCodeQualityEnabled;
            set
            {
                if (this.userStorageSettingsService == null || userStorageSettingsService.SnykCodeQualityEnabled == value)
                {
                    return;
                }

                this.userStorageSettingsService.SnykCodeQualityEnabled = value;
            }
        }

        public bool BinariesAutoUpdate
        {
            get => this.userStorageSettingsService.BinariesAutoUpdate;
            set
            {
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.BinariesAutoUpdate == value)
                {
                    return;
                }
                this.userStorageSettingsService.BinariesAutoUpdate = value;
            }
        }

        public string CliCustomPath
        {
            get => this.userStorageSettingsService.CliCustomPath;
            set
            {
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.CliCustomPath == value)
                {
                    return;
                }
                this.userStorageSettingsService.CliCustomPath = value;
            }
        }

        public string CliReleaseChannel
        {
            get => this.userStorageSettingsService.CliReleaseChannel;
            set
            {
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.CliReleaseChannel == value)
                {
                    return;
                }
                this.userStorageSettingsService.CliReleaseChannel = value;
            }
        }
        public string CliDownloadUrl
        {
            get => this.userStorageSettingsService.CliDownloadUrl;
            set
            {
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.CliDownloadUrl == value)
                {
                    return;
                }
                this.userStorageSettingsService.CliDownloadUrl = value;
            }
        }

        public ISet<string> TrustedFolders
        {
            get => this.userStorageSettingsService.TrustedFolders;
            set
            {
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.TrustedFolders == value)
                    return;
                this.userStorageSettingsService.TrustedFolders = value;
            }
        }

        public bool EnableDeltaFindings
        {
            get => this.userStorageSettingsService.EnableDeltaFindings;
            set
            {
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.EnableDeltaFindings == value)
                    return;
                this.userStorageSettingsService.EnableDeltaFindings = value;
                userStorageSettingsService.SaveSettings();
            }
        }

        public List<FolderConfig> FolderConfigs
        {
            get => this.userStorageSettingsService.FolderConfigs;
            set
            {
                if (this.userStorageSettingsService == null)
                    return;
                this.userStorageSettingsService.FolderConfigs = value;
                userStorageSettingsService.SaveSettings();
                this.SettingsChanged?.Invoke(this, new SnykSettingsChangedEventArgs());
            }
        }

        public event EventHandler<SnykSettingsChangedEventArgs> SettingsChanged;

        /// <summary>
        /// Gets a value indicating whether additional options.
        /// Get this data using <see cref="SnykUserStorageSettingsService"/>.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<string> GetAdditionalOptionsAsync() => await this.serviceProvider.UserStorageSettingsService.GetAdditionalOptionsAsync();

        /// <summary>
        /// Gets a value indicating whether is scan all projects enabled via <see cref="SnykUserStorageSettingsService"/>.
        /// Get this data using <see cref="SnykUserStorageSettingsService"/>.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<bool> IsScanAllProjectsAsync() => await this.userStorageSettingsService.GetIsAllProjectsEnabledAsync();


        public string CurrentCliVersion
        {
            get => this.userStorageSettingsService.CurrentCliVersion;
            set
            {
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.CurrentCliVersion == value)
                {
                    return;
                }
                this.userStorageSettingsService.CurrentCliVersion = value;
            }
        }

        public SastSettings SastSettings { get; set; }
        
        public bool AnalyticsPluginInstalledSent
        {
            get => this.userStorageSettingsService.AnalyticsPluginInstalledSent;
            set => this.userStorageSettingsService.AnalyticsPluginInstalledSent = value;
        }

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

        public void SaveSettings()
        {
            this.userStorageSettingsService.SaveSettings();
        }
    }
}
