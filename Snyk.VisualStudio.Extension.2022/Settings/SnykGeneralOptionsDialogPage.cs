using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.Common;
using Snyk.Common.Authentication;
using Snyk.Common.Service;
using Snyk.Common.Settings;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.UI.Notifications;

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

        private AuthenticationToken apiToken;

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
        public AuthenticationToken ApiToken => this.apiToken ?? AuthenticationToken.EmptyToken;
        
        public AuthenticationType AuthenticationMethod
        {
            get => this.userStorageSettingsService.AuthenticationMethod;
            set
            {
                if (this.userStorageSettingsService == null || this.userStorageSettingsService.AuthenticationMethod == value)
                    return;
                this.userStorageSettingsService.AuthenticationMethod = value;
                // When changing the Token Type, the token is invalidated
                InvalidateCurrentToken();
                this.FireSettingsChangedEvent();
            }
        }

        private SastSettings sastSettings;

        private string RefreshToken()
        {
            Logger.Information("Attempting to refresh OAuth token");
            var cli = this.ServiceProvider?.NewCli();
            if (cli == null)
            {
                Logger.Information("Couldn't get CLI. Aborting");
                return string.Empty;
            }

            try
            {
                cli.RunCommand("whoami --experimental");
            }
            catch (AuthenticationException ex)
            {
                Logger.Error("Failed to refresh access token: {Message}", ex.Message);
                InvalidateCurrentToken();
                NotificationService.Instance?.ShowErrorInfoBar("Failed to refresh Access token");
                return string.Empty;
            }

            var token = cli.GetApiToken();
            return token;
        }

        public void SetApiToken(string apiTokenString)
        {
            if (this.apiToken?.ToString() == apiTokenString)
            {
                return;
            }

            SetApiToken(CreateAuthenticationToken(apiTokenString));
        }

        private void SetApiToken(AuthenticationToken token)
        {
            this.apiToken = token;
            this.apiToken.TokenRefresher = RefreshToken;
            this.FireSettingsChangedEvent();
        }

        private AuthenticationToken CreateAuthenticationToken(string token)
        {
            var apiEndpointResolver = new ApiEndpointResolver(this);
            var type = apiEndpointResolver.AuthenticationMethod;
            
            var tokenObj = new AuthenticationToken(type, token)
            {
                TokenRefresher = RefreshToken
            };

            return tokenObj;
        }
        
        /// <summary>
        /// Checks if the current endpoint is a fedramp endpoint
        /// </summary>
        /// <returns></returns>
        public bool IsFedramp()
        {
            var endpoint = this.customEndpoint;
            if (endpoint.IsNullOrEmpty())
            {
                return false;
            }

            var endpointUri = new Uri(endpoint);
            return endpointUri.Host.ToLower().EndsWith("snykgov.io");
        }

        /// <summary>
        /// Checks if the current endpoint permits sending external analytics events
        /// </summary>
        /// <returns></returns>
        public bool IsAnalyticsPermitted()
        {
            var endpointUri = new Uri(this.GetCustomApiEndpoint());

            var permittedHosts = new string[] { "api.snyk.io", "api.us.snyk.io" };
            return permittedHosts.Contains(endpointUri.Host.ToLower());
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
                // When changing the API endpoint, the API token is invalidated
                InvalidateCurrentToken();

                this.customEndpoint = newApiEndpoint;
                this.FireSettingsChangedEvent();
            }
        }

        private void InvalidateCurrentToken()
        {
            this.apiToken = AuthenticationToken.EmptyToken;
            var cli = this.ServiceProvider?.NewCli();
            cli?.UnsetApiToken();
        }

        /// <inheritdoc/>
        public string SnykCodeSettingsUrl => $"{this.GetBaseAppUrl()}/manage/snyk-code";

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

        /// <summary>
        /// Gets or sets a value indicating whether use analytics enabled.
        /// Save data via <see cref="SnykUserStorageSettingsService"/>.
        /// </summary>
        public bool UsageAnalyticsEnabled
        {
            get => this.userStorageSettingsService.IsUsageAnalyticsEnabled();
            set => this.userStorageSettingsService?.SaveUsageAnalyticsEnabled(value);
        }

        /// <inheritdoc/>
        public bool BinariesAutoUpdate
        {
            get => this.userStorageSettingsService.BinariesAutoUpdate;
            set
            {
                if (this.userStorageSettingsService != null)
                {
                    this.userStorageSettingsService.BinariesAutoUpdate = value;
                }
            }
        }

        public string CliCustomPath
        {
            get => this.userStorageSettingsService.CliCustomPath;
            set
            {
                if (this.userStorageSettingsService != null)
                {
                    this.userStorageSettingsService.CliCustomPath = value;
                    this.FireSettingsChangedEvent();
                }
            }
        }

        /// <inheritdoc/>
        public string AnonymousId
        {
            get => this.userStorageSettingsService.GetAnonymousId();
            set => this.userStorageSettingsService?.SaveAnonymousId(value);
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
                    this.generalSettingsUserControl = new SnykGeneralSettingsUserControl(this.serviceProvider.ApiService)
                    {
                        OptionsDialogPage = this,
                    };

                    this.generalSettingsUserControl.Initialize();
                }

                return this.generalSettingsUserControl;
            }
        }

        public SnykUser SnykUser { get; set; }

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

            var componentModel = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;
            Assumes.Present(componentModel);

            const int errorMessageMaxLength = 100;

            Logger.Information("Enter Authenticate method");
            var cli = this.ServiceProvider.NewCli();
            if (!cli.IsCliFileFound())
            {
                ThrowFileNotFoundException();
            }
            try
            {
                // Pull token from configuration. If the token is invalid, attempt to authenticate and get a new token.
                var apiTokenString = cli.GetApiToken();
                var token = CreateAuthenticationToken(apiTokenString);
                if (!token.IsValid() && token.Type == AuthenticationType.OAuth)
                {
                    // Before re-authenticating attempt to refresh current token
                    token = CreateAuthenticationToken(token.Refresh());
                }
                if (!token.IsValid())
                {
                    Logger.Information("Api token is invalid. Attempting to authenticate via snyk auth");
                    try
                    {
                        this.ServiceProvider.NewCli().Authenticate();
                    }
                    catch (AuthenticationException e)
                    {
                        var shortMessage = e.Message.Length > errorMessageMaxLength ? e.Message.Substring(0, errorMessageMaxLength) + "..." : e.Message;
                        Logger.Information("Snyk failed to authenticate: {Message}", e.Message);
                        NotificationService.Instance.ShowErrorInfoBar($"Snyk failed to authenticate: {shortMessage}");
                        return false;
                    }
                    catch (Exception e)
                    {
                        var shortMessage = e.Message.Length > errorMessageMaxLength ? e.Message.Substring(0, errorMessageMaxLength) + "..." : e.Message;
                        Logger.Information("Error in Snyk authentication: {Message}", e.Message);
                        NotificationService.Instance.ShowErrorInfoBar($"Snyk failed to authenticate: {shortMessage}");
                        return false;
                    }

                    apiTokenString = this.ServiceProvider.NewCli().GetApiToken();
                    token = CreateAuthenticationToken(apiTokenString);
                    
                    if (!token.IsValid()) // Token is still invalid after the authentication attempt.
                    {
                        NotificationService.Instance.ShowErrorInfoBar("Snyk failed to authenticate");
                        return false;
                    }
                }

                // Token is valid, store it and return true
                this.SetApiToken(token);

                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    SnykUser = await serviceProvider.ApiService.GetUserAsync();
                });
                return true;

            }
            catch (Exception e)
            {
                Logger.Error(e, "Setup api token in general settings");
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
