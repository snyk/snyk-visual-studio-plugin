using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Serilog;
using Snyk.Code.Library.Service;
using Snyk.Common;
using Snyk.Common.Service;
using Snyk.Common.Settings;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.Theme;
using Snyk.VisualStudio.Extension.UI;
using Snyk.VisualStudio.Extension.UI.Notifications;
using Snyk.VisualStudio.Extension.UI.Toolwindow;

namespace Snyk.VisualStudio.Extension.Service
{
    /// <summary>
    /// Main logic for Snyk extension.
    /// </summary>
    public class SnykService : ISnykServiceProvider, ISnykService, ICliProvider
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykService>();

        private readonly IAsyncServiceProvider serviceProvider;
        private readonly string vsVersion;
        private SettingsManager settingsManager;

        private SnykVsThemeService vsThemeService;

        private SnykTasksService tasksService;

        private DTE2 dte;

        private SnykIdeAnalyticsService ideAnalyticsService;

        private SnykUserStorageSettingsService userStorageSettingsService;

        private ISnykCodeService snykCodeService;

        private IOssService ossService;

        private ISnykApiService apiService;

        private ISentryService sentryService;

        private IWorkspaceTrustService workspaceTrustService;
        
        private ILanguageClientManager languageClientManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykService"/> class.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider implementation.</param>
        /// <param name="vsVersion">The version of the IDE</param>
        public SnykService(IAsyncServiceProvider serviceProvider, string vsVersion = "")
        {
            this.serviceProvider = serviceProvider;
            this.vsVersion = vsVersion;
        }

        /// <summary>
        /// Gets Snyk options implementation.
        /// </summary>
        public ISnykOptions Options => this.Package.GeneralOptionsDialogPage;

        /// <summary>
        /// Gets solution service.
        /// </summary>
        public ISolutionService SolutionService => SnykSolutionService.Instance;

        /// <summary>
        /// Gets solution service.
        /// </summary>
        public IWorkspaceTrustService WorkspaceTrustService => this.workspaceTrustService;

        /// <summary>
        /// Gets Tasks service.
        /// </summary>
        public SnykTasksService TasksService => this.tasksService;

        /// <summary>
        /// Gets VS Settings manager.
        /// </summary>
        public SettingsManager SettingsManager => this.settingsManager;

        /// <summary>
        /// Gets VS dte instance.
        /// </summary>
        public DTE2 DTE => this.dte;

        /// <summary>
        /// Gets Snyk Extension package intance.
        /// </summary>
        public SnykVSPackage Package => this.serviceProvider as SnykVSPackage;

        /// <summary>
        /// Gets implementation of IAsyncServiceProvider.
        /// </summary>
        public IAsyncServiceProvider AsyncServiceProvider => this.serviceProvider;

        /// <summary>
        /// Gets VS theme service.
        /// </summary>
        public SnykVsThemeService VsThemeService => this.vsThemeService;

        public SnykIdeAnalyticsService SnykIdeAnalyticsService
        {
            get
            {
                if (ideAnalyticsService == null)
                {
                    ideAnalyticsService =
                        new SnykIdeAnalyticsService(Package,  this, TasksService, SolutionService);
                }

                return ideAnalyticsService;
            }
        }

        /// <summary>
        /// Gets user storage settings service instance.
        /// </summary>
        public SnykUserStorageSettingsService UserStorageSettingsService
        {
            get
            {
                if (this.userStorageSettingsService == null)
                {
                    string settingsFilePath = Path.Combine(SnykExtension.GetExtensionDirectoryPath(), "settings.json");

                    this.userStorageSettingsService = new SnykUserStorageSettingsService(settingsFilePath, this);
                }

                return this.userStorageSettingsService;
            }
        }

        /// <inheritdoc/>
        public ISnykCodeService SnykCodeService
        {
            get
            {
                if (this.snykCodeService == null)
                {
                    this.SetupSnykCodeService();
                }

                return this.snykCodeService;
            }
        }

        /// <inheritdoc/>
        public ISnykApiService ApiService
        {
            get
            {
                if (this.apiService == null)
                {
                    this.apiService = new SnykApiService(this.Options, this.vsVersion, SnykExtension.Version);

                    this.Options.SettingsChanged += this.OnSettingsChanged;
                }

                return this.apiService;
            }
        }

        /// <inheritdoc/>
        public IOssService OssService
        {
            get
            {
                if (this.ossService == null)
                {
                    this.ossService = new OssService(this);
                }

                return this.ossService;
            }
        }

        /// <inheritdoc/>
        public ISentryService SentryService
        {
            get
            {
                if (this.sentryService == null)
                {
                    this.sentryService = new SentryService(this);
                }

                return this.sentryService;
            }
        }

        /// <inheritdoc/>
        public SnykToolWindowControl ToolWindow => this.Package.ToolWindowControl;

        public ApiEndpointResolver ApiEndpointResolver => new ApiEndpointResolver(this.Options);

        /// <summary>
        /// Get Visual Studio service by type.
        /// </summary>
        /// <param name="serviceType">Needed service type.</param>
        /// <returns>Result VS service instance</returns>
        public async Task<object> GetServiceAsync(Type serviceType) =>
            await this.serviceProvider.GetServiceAsync(serviceType);

        /// <summary>
        /// Initialize service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token instance.</param>
        /// <returns>Task.</returns>
        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logger.Information("Initialize Snyk services");
                Logger.Information("Plugin version is {Version}", SnykExtension.Version);
                await this.Package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                this.settingsManager = new ShellSettingsManager(this.Package);
                this.vsThemeService = new SnykVsThemeService(this);

                await this.vsThemeService.InitializeAsync();
                await SnykToolWindowCommand.InitializeAsync(this);
                await SnykTasksService.InitializeAsync(this);

                this.dte = await this.serviceProvider.GetServiceAsync(typeof(DTE)) as DTE2;
                await SnykSolutionService.Instance.InitializeAsync(this);


                this.tasksService = SnykTasksService.Instance;
                this.workspaceTrustService = new WorkspaceTrustService(this.UserStorageSettingsService);

                NotificationService.Initialize(this);
                VsStatusBar.Initialize(this);
                VsCodeService.Initialize();

                SnykIdeAnalyticsService.Initialize();

                Logger.Information("Leave SnykService.InitializeAsync");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error on initialize Snyk service");
            }
        }

        /// <summary>
        /// Create new instance of SnykCli class with Options and Logger parameters.
        /// </summary>
        /// <returns>New SnykCli instance.</returns>
        public ICli NewCli() => new SnykCli(this.Options, this.vsVersion);

        private void OnSettingsChanged(object sender, SnykSettingsChangedEventArgs e) => this.SetupSnykCodeService();

        private void SetupSnykCodeService()
        {
            try
            {
                var options = this.Options;
                string endpoint = new ApiEndpointResolver(this.Options).GetSnykCodeApiUrl();
                var httpClient = HttpClientFactory.NewHttpClient(options.ApiToken, endpoint)
                    .WithUserAgent(this.vsVersion, SnykExtension.Version);

                this.snykCodeService = CodeServiceFactory.CreateSnykCodeService(
                    options.ApiToken,
                    endpoint,
                    this.SolutionService.FileProvider,
                    SnykExtension.IntegrationName,
                    options.Organization ?? string.Empty,
                    httpClient);

                VsStatusBarNotificationService.Instance.InitializeEventListeners(this.snykCodeService, options);
            }
            catch (Exception e)
            {
                Logger.Error(e, string.Empty);
            }
        }

        private ICli _cli;
        public ICli Cli {
            get {
                _cli ??= NewCli();
                return _cli;
            } 
        }
    }
}