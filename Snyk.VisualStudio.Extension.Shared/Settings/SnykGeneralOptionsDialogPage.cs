namespace Snyk.VisualStudio.Extension.Shared.Settings
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security.Authentication;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Microsoft.VisualStudio.ComponentModelHost;
    using Microsoft;
    using Microsoft.VisualStudio.Shell;
    using Serilog;
    using Snyk.Common;
    using Snyk.Common.Authentication;
    using Snyk.Common.Service;
    using Snyk.Common.Settings;
    using Snyk.VisualStudio.Extension.Shared.Language;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using Snyk.VisualStudio.Extension.Shared.UI.Notifications;

    /// <summary>
    /// Snyk general settings page.
    /// </summary>
    [Guid("d45468c1-33d2-4dca-9780-68abaedf95e7")]
    [ComVisible(true)]
    public class SnykGeneralOptionsDialogPage : DialogPage, ISnykOptions
    {
        public String Application { get; set; }
        public String ApplicationVersion { get; set; }
        public String IntegrationName { get; } = SnykExtension.IntegrationName;
        public String IntegrationVersion { get; } = SnykExtension.Version;
        public String IntegrationEnvironment { get; set; }
        public String IntegrationEnvironmentVersion { get; set;}
        
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

        private SastSettings sastSettings;

        private string RefreshToken()
        {
            var cli = this.ServiceProvider?.NewCli();
            cli.RunCommand("whoami --experimental");
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

            var tokenObj = new AuthenticationToken(type, token);
            tokenObj.TokenRefresher = RefreshToken;
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
            var endpointUri = new Uri(this.GetBaseAppURL());

            var permittedHosts = new string[] { "app.snyk.io", "app.us.snyk.io" };
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

                if (this.customEndpoint == value)
                {
                    return;
                }

                // When changing the API endpoint, the API token is invalidated
                this.apiToken = AuthenticationToken.EmptyToken;
                var cli = this.ServiceProvider?.NewCli();
                cli?.UnsetApiToken(); // This setter can be called before initialization, so ServiceProvider can be null

                this.customEndpoint = value;
                this.FireSettingsChangedEvent();
            }
        }

        /// <inheritdoc/>
        public string SnykCodeSettingsUrl => $"{this.GetBaseAppURL()}/manage/snyk-code";

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
                

                var languageServerClientManager = componentModel.GetService<ILanguageClientManager>();

                languageServerClientManager.SetOptions(cli.GetCliPath(), token.ToString());
                ThreadHelper.JoinableTaskFactory.Run(languageServerClientManager.StartServerAsync);

                return true;

            }
            catch (Exception e)
            {
                Logger.Error(e, "Setup api token in general settings");
                return false;
            }
        }

        private void FireSettingsChangedEvent() => this.SettingsChanged?.Invoke(this, new SnykSettingsChangedEventArgs());

        private string GetBaseAppURL()
        {
            var endpoint = this.customEndpoint.IsNullOrEmpty() ? "https://app.snyk.io" : this.customEndpoint.RemoveTrailingSlashes();
            Uri uri = new Uri(endpoint);

            if (!uri.Host.StartsWith("app") && (uri.Host.EndsWith("snyk.io") || uri.Host.EndsWith("snykgov.io")))
            {
                return endpoint.Replace("https://", "https://app.").RemoveFromEnd("/api");
            }
            else if (uri.Host.StartsWith("app") && (uri.Host.EndsWith("snyk.io") || uri.Host.EndsWith("snykgov.io")))
            {
                return endpoint.RemoveFromEnd("/api");
            }
            else
            {
                return "https://app.snyk.io";
            }
        }

        private void ThrowFileNotFoundException()
        {
            const string cliNotFound = "CLI not found";
            Logger.Information(cliNotFound);
            throw new FileNotFoundException(cliNotFound);
        }
    }
}
