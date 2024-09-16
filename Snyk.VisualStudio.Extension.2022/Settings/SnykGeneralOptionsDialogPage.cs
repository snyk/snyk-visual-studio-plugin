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
using Snyk.VisualStudio.Extension.Download;
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

        private ISnykServiceProvider serviceProvider;

        private SnykUserStorageSettingsService userStorageSettingsService;

        private SnykGeneralSettingsUserControl generalSettingsUserControl;

        private string organization;

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
                this.FireSettingsChangedEvent();
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
                this.FireSettingsChangedEvent();
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
                this.FireSettingsChangedEvent();
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
                this.FireSettingsChangedEvent();
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
                if (this.sastSettings == value)
                {
                    return;
                }

                this.sastSettings = value;
            }
        }

        public async Task OnAuthenticationSuccessfulAsync(string token)
        {
            await this.GeneralSettingsUserControl.OnAuthenticationSuccessfulAsync(token);
        }

        public async Task OnAuthenticationFailedAsync(string errorMessage)
        {
            await this.GeneralSettingsUserControl.OnAuthenticationFailAsync(errorMessage);
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
                this.FireSettingsChangedEvent();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether ignore unknown CA.
        /// </summary>
        public bool IgnoreUnknownCA
        {
            get => this.userStorageSettingsService.IgnoreUnknownCA;
            set
            {
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.IgnoreUnknownCA == value)
                {
                    return;
                }
                this.userStorageSettingsService.IgnoreUnknownCA = value;
                this.FireSettingsChangedEvent();
            }
        }

        /// <inheritdoc/>
        public bool OssEnabled
        {
            get => this.userStorageSettingsService.IsOssEnabled();
            set
            {
                if (this.userStorageSettingsService?.IsOssEnabled() == value)
                {
                    return;
                }

                this.userStorageSettingsService?.SaveOssEnabled(value);
                this.FireSettingsChangedEvent();
            }
        }

        public bool IacEnabled
        {
            get => this.userStorageSettingsService.IsIacEnabled();
            set
            {
                if (this.userStorageSettingsService?.IsIacEnabled() == value)
                {
                    return;
                }

                this.userStorageSettingsService?.SaveIacEnabled(value);
                this.FireSettingsChangedEvent();
            }
        }

        /// <inheritdoc/>
        public bool SnykCodeSecurityEnabled
        {
            get => this.userStorageSettingsService.IsSnykCodeSecurityEnabled();
            set
            {
                if (this.userStorageSettingsService?.IsSnykCodeSecurityEnabled() == value)
                {
                    return;
                }

                this.userStorageSettingsService?.SaveSnykCodeSecurityEnabled(value);
                this.FireSettingsChangedEvent();
            }
        }

        /// <inheritdoc/>
        public bool SnykCodeQualityEnabled
        {
            get => this.userStorageSettingsService.IsSnykCodeQualityEnabled();
            set
            {
                if (this.userStorageSettingsService?.IsSnykCodeQualityEnabled() == value)
                {
                    return;
                }

                this.userStorageSettingsService?.SaveSnykCodeQualityEnabled(value);
                this.FireSettingsChangedEvent();
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
                HandleCliCustomPathChange();
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

        /// <summary>
        /// Gets a value indicating whether General Settings control.
        /// </summary>
        protected override IWin32Window Window => this.GeneralSettingsUserControl;

        protected override void OnClosed(EventArgs e)
        {
            if (generalSettingsUserControl == null)
                return;
            ResetControlScrollSettings(generalSettingsUserControl);
            HandleCliDownload();
        }

        private void HandleCliCustomPathChange()
        {
            if (SnykCliDownloader.IsCliFileFound(this.CliCustomPath) && LanguageClientHelper.IsLanguageServerReady())
            {
                // Cancel running tasks
                serviceProvider.TasksService.CancelTasks();
                ThreadHelper.JoinableTaskFactory.RunAsync(async()=> await LanguageClientHelper.LanguageClientManager().RestartServerAsync()).FireAndForget();
            }
        }

        private void HandleCliDownload()
        {
            var releaseChannel = generalSettingsUserControl.GetReleaseChannel().Trim();
            var downloadUrl = generalSettingsUserControl.GetCliDownloadUrl().Trim();
            var manageBinariesAutomatically = generalSettingsUserControl.GetManageBinariesAutomatically();
            if (!manageBinariesAutomatically)
            {
                this.userStorageSettingsService.SaveCurrentCliVersion(string.Empty);
            }
            if (this.CliReleaseChannel != releaseChannel || this.CliDownloadUrl != downloadUrl || this.BinariesAutoUpdate != manageBinariesAutomatically)
            {
                this.CliDownloadUrl = downloadUrl;
                this.CliReleaseChannel = releaseChannel;
                this.BinariesAutoUpdate = manageBinariesAutomatically;
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
        public bool Authenticate()
        {
            Logger.Information("Enter Authenticate method");
            var cli = this.ServiceProvider.NewCli();
            if (!cli.IsCliFileFound())
            {
                ThrowFileNotFoundException();
            }
            try
            {
                if (!LanguageClientHelper.IsLanguageServerReady())
                {
                    Logger.Error("Language Server is not initialized yet.");
                    return false;
                }
                if (ApiToken.IsValid()) 
                    return true;
                
                Logger.Information("Api token is invalid. Attempting to authenticate via snyk auth");
                ThreadHelper.JoinableTaskFactory.Run(async ()=>
                {
                    await ServiceProvider.Package.LanguageClientManager.InvokeLogout(SnykVSPackage.Instance.DisposalToken);
                    var token = await ServiceProvider.Package.LanguageClientManager.InvokeLogin(SnykVSPackage.Instance.DisposalToken);
                    ApiToken = CreateAuthenticationToken(token);
                });
                return true;

            }
            catch (Exception e)
            {
                Logger.Error(e, "Couldn't execute Invoke Login through LS.");
                return false;
            }
        }
        private void FireSettingsChangedEvent() => this.SettingsChanged?.Invoke(this, new SnykSettingsChangedEventArgs());

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
