using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.Common;
using Snyk.Common.Authentication;
using Snyk.Common.Service;
using Snyk.Common.Settings;
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

        private string customEndpoint;

        private string organization;

        private static readonly ILogger Logger = LogManager.ForContext<SnykGeneralOptionsDialogPage>();

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
            get => this.customEndpoint;
            set
            {
                if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
                {
                    Logger.Warning("Custom endpoint value is not a well-formed URI. Setting custom endpoint to empty string");
                    value = string.Empty;
                }

                var newApiEndpoint = ApiEndpointResolver.TranslateOldApiToNewApiEndpoint(value);
                if (this.customEndpoint == newApiEndpoint)
                {
                    return;
                }

                this.customEndpoint = newApiEndpoint;
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
            get => this.organization;
            set
            {
                if (this.organization == value)
                {
                    return;
                }

                this.organization = value;
                this.FireSettingsChangedEvent();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether ignore unknown CA.
        /// </summary>
        public bool IgnoreUnknownCA { get; set; }

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

        /// <inheritdoc/>
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
                // TODO: Handle CLI Path Change
            }
        }

        /// <summary>
        /// Gets a value indicating whether General Settings control.
        /// </summary>
        protected override IWin32Window Window => this.GeneralSettingsUserControl;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (generalSettingsUserControl == null)
                return;
            ResetControlScrollSettings(generalSettingsUserControl);
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
                    await ServiceProvider.Package.LanguageClientManager.InvokeLogout(CancellationToken.None);
                    var token = await ServiceProvider.Package.LanguageClientManager.InvokeLogin(CancellationToken.None);
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
            return string.IsNullOrEmpty(customEndpoint) ? ApiEndpointResolver.DefaultApiEndpoint : ApiEndpointResolver.TranslateOldApiToNewApiEndpoint(customEndpoint);
        }

        public string GetBaseAppUrl()
        {
            if (string.IsNullOrEmpty(customEndpoint))
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
