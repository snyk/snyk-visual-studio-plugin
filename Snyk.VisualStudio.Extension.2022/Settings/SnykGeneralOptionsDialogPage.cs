using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.Common;
using Snyk.Common.Authentication;
using Snyk.Common.Service;
using Snyk.Common.Settings;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Snyk general settings page.
    /// </summary>
    [Guid("d45468c1-33d2-4dca-9780-68abaedf95e7")]
    [ComVisible(true)]
    public class SnykGeneralOptionsDialogPage : DialogPage, ISnykOptions
    {
        public string Application { get; set; }
        public string ApplicationVersion { get; set; }
        public string IntegrationName { get; } = SnykExtension.IntegrationName;
        public string IntegrationVersion { get; } = SnykExtension.Version;
        public string IntegrationEnvironment { get; set; }
        public string IntegrationEnvironmentVersion { get; set;}
        
        public string DeviceId
        {
            get => this.userStorageSettingsService.DeviceId;
            set => this.userStorageSettingsService.DeviceId = value;
        }

        private ISnykServiceProvider serviceProvider;

        private SnykUserStorageSettingsService userStorageSettingsService;

        private SnykGeneralSettingsUserControl generalSettingsUserControl;

        private static readonly ILogger Logger = LogManager.ForContext<SnykGeneralOptionsDialogPage>();

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

        /// <inheritdoc/>
        public event EventHandler<SnykSettingsChangedEventArgs> SettingsChanged;

        /// <summary>
        /// Gets a value indicating whether service provider.
        /// </summary>
        public ISnykServiceProvider ServiceProvider => this.serviceProvider;

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

        /// <summary>
        /// Gets or sets a value indicating whether API token.
        /// </summary>
        public AuthenticationToken ApiToken
        {
            get => CreateAuthenticationToken(this.userStorageSettingsService.Token);
            set
            {
                var tokenAsString = value.ToString();
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.Token == tokenAsString)
                    return;
                this.userStorageSettingsService.Token = tokenAsString;
                userStorageSettingsService.SaveSettings();
                FireSettingsChangedEvent();
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
                this.GeneralSettingsUserControl.InvalidateApiToken();
                ApiToken = AuthenticationToken.EmptyToken;
                userStorageSettingsService.SaveSettings();
                FireSettingsChangedEvent();
            }
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

        private AuthenticationToken CreateAuthenticationToken(string token)
        {
            var tokenObj = new AuthenticationToken(this.AuthenticationMethod, token);
            return tokenObj;
        }

        /// <summary>
        /// Gets or sets a value indicating whether Custom endpoint.
        /// </summary>
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
                FireSettingsChangedEvent();
            }
        }

        /// <inheritdoc/>
        public string SnykCodeSettingsUrl => $"{this.GetBaseAppUrl()}/manage/snyk-code";
        
        private SastSettings sastSettings;
        public SastSettings SastSettings
        {
            get => this.sastSettings;

            set
            {
                this.sastSettings = value;
            }
        }

        public bool AnalyticsPluginInstalledSent
        {
            get => this.userStorageSettingsService.AnalyticsPluginInstalledSent; 
            set => this.userStorageSettingsService.AnalyticsPluginInstalledSent = value;
        }

        public async Task HandleAuthenticationSuccess(string token, string apiUrl)
        {
            await this.GeneralSettingsUserControl.HandleAuthenticationSuccess(token, apiUrl);
        }

        public async Task HandleFailedAuthentication(string errorMessage)
        {
            await this.GeneralSettingsUserControl.HandleFailedAuthentication(errorMessage);
        }
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
                FireSettingsChangedEvent();
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
                FireSettingsChangedEvent();
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

        /// <inheritdoc/>
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

        /// <summary>
        /// Gets a value indicating whether General Settings control.
        /// </summary>
        protected override IWin32Window Window => this.GeneralSettingsUserControl;

        // This method is used when the user clicks "Ok"
        public override void SaveSettingsToStorage()
        {
            HandleCliDownload();
            this.userStorageSettingsService?.SaveSettings();
            this.FireSettingsChangedEvent();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (generalSettingsUserControl == null)
                return;
            ResetControlScrollSettings(generalSettingsUserControl);
        }

        private void HandleCliDownload()
        {
            var releaseChannel = generalSettingsUserControl.GetReleaseChannel().Trim();
            var downloadUrl = generalSettingsUserControl.GetCliDownloadUrl().Trim();
            var manageBinariesAutomatically = generalSettingsUserControl.GetManageBinariesAutomatically();
            if (!manageBinariesAutomatically)
            {
                this.userStorageSettingsService.CurrentCliVersion = string.Empty;
                this.BinariesAutoUpdate = false;
                serviceProvider.TasksService.CancelDownloadTask();
                // Language Server restart will happen on DownloadCancelled Event.
                return;
            }
            if (this.CliReleaseChannel != releaseChannel || this.CliDownloadUrl != downloadUrl || this.BinariesAutoUpdate != manageBinariesAutomatically)
            {
                this.CliDownloadUrl = downloadUrl;
                this.CliReleaseChannel = releaseChannel;
                this.BinariesAutoUpdate = manageBinariesAutomatically;
                serviceProvider.TasksService.CancelDownloadTask();
                this.serviceProvider.TasksService.Download();
            }
        }

        private void ResetControlScrollSettings(UserControl control)
        {
            control.VerticalScroll.Value = 0;
            control.HorizontalScroll.Value = 0;
            control.AutoScroll = true;
        }

        private SnykGeneralSettingsUserControl GeneralSettingsUserControl
        {
            get
            {
                if (this.generalSettingsUserControl == null)
                {
                    this.generalSettingsUserControl = new SnykGeneralSettingsUserControl()
                    {
                        OptionsDialogPage = this,
                    };

                    this.generalSettingsUserControl.Initialize();
                }

                return this.generalSettingsUserControl;
            }
        }

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

        /// <summary>
        /// Initialize <see cref="SnykGeneralOptionsDialogPage"/>.
        /// </summary>
        /// <param name="provider">Snyk service provider.</param>
        public void Initialize(ISnykServiceProvider provider)
        {
            this.serviceProvider = provider;

            this.userStorageSettingsService = this.serviceProvider.UserStorageSettingsService;
        }

        /// <inheritdoc />
        public void Authenticate()
        {
            Logger.Information("Enter Authenticate method");
            if (!SnykCli.IsCliFileFound(this.CliCustomPath))
            {
                ThrowFileNotFoundException();
            }
            try
            {
                if (!LanguageClientHelper.IsLanguageServerReady())
                {
                    Logger.Error("Language Server is not initialized yet.");
                    return;
                }
                if (ApiToken.IsValid()) 
                    return;
                
                Logger.Information("Api token is invalid. Attempting to authenticate via snyk auth");

                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ServiceProvider.Package.LanguageClientManager.InvokeLogout(SnykVSPackage.Instance
                        .DisposalToken);
                    await ServiceProvider.Package.LanguageClientManager.InvokeLogin(SnykVSPackage.Instance
                        .DisposalToken);
                }).FireAndForget();

                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    AuthDialogWindow.Instance.ShowDialog();
                });
            }
            catch (Exception e)
            {
                Logger.Error(e, "Couldn't execute Invoke Login through LS.");
            }
        }

        public void FireSettingsChangedEvent() => this.SettingsChanged?.Invoke(this, new SnykSettingsChangedEventArgs());

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

        private void ThrowFileNotFoundException()
        {
            const string cliNotFound = "CLI not found";
            Logger.Information(cliNotFound);
            throw new FileNotFoundException(cliNotFound);
        }
    }
}
