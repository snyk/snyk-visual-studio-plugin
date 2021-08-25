﻿namespace Snyk.VisualStudio.Extension.Service
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using EnvDTE;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Shell.Settings;
    using Snyk.Code.Library.Api;
    using Snyk.Code.Library.Service;
    using Snyk.VisualStudio.Extension.CLI;
    using Snyk.VisualStudio.Extension.Settings;
    using Snyk.VisualStudio.Extension.SnykAnalytics;
    using Snyk.VisualStudio.Extension.Theme;
    using Snyk.VisualStudio.Extension.UI;
    using Snyk.VisualStudio.Extension.UI.Notifications;
    using Snyk.VisualStudio.Extension.UI.Toolwindow;
    using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Main logic for Snyk extension.
    /// </summary>
    public class SnykService : ISnykServiceProvider, ISnykService
    {
        private readonly IAsyncServiceProvider serviceProvider;

        private SnykActivityLogger activityLogger;

        private SettingsManager settingsManager;

        private SnykVsThemeService vsThemeService;

        private SnykTasksService tasksService;

        private SnykSolutionService solutionService;

        private DTE dte;

        private SnykAnalyticsService analyticsService;

        private SnykUserStorageSettingsService userStorageSettingsService;

        private SnykCodeService snykCodeService;

        private FiltersService filterService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykService"/> class.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider implementation.</param>
        public SnykService(IAsyncServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

        /// <summary>
        /// Gets VS logger.
        /// </summary>
        public SnykActivityLogger ActivityLogger => this.activityLogger;

        /// <summary>
        /// Gets Snyk options implementation.
        /// </summary>
        public ISnykOptions Options => this.Package.GeneralOptionsDialogPage;

        /// <summary>
        /// Gets solution service.
        /// </summary>
        public SnykSolutionService SolutionService => this.solutionService;

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
        public DTE DTE => this.dte;

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

        /// <summary>
        /// Gets Analytics service instance. If analytics service not created yet it will create it and return.
        /// </summary>
        public SnykAnalyticsService AnalyticsService
        {
            get
            {
                if (this.analyticsService == null)
                {
                    this.activityLogger.LogInformation("Initialize Snyk Segment Analytics Service.");

                    this.analyticsService = new SnykAnalyticsService
                    {
                        Logger = this.activityLogger,
                    };

                    this.analyticsService.Initialize();
                }

                return this.analyticsService;
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
                    this.userStorageSettingsService = new SnykUserStorageSettingsService(this);
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
                    var codeClient = new SnykCodeClient(SnykExtension.GetAppSettings().SnykCodeApiEndpoinUrl, this.Options.ApiToken);

                    this.filterService = new FiltersService(codeClient);

                    var bundleService = new BundleService(codeClient);
                    var analysisService = new AnalysisService(codeClient);

                    this.snykCodeService = new SnykCodeService(bundleService, analysisService, this.filterService);
                }

                return this.snykCodeService;
            }
        }

        /// <summary>
        /// Show Snyk tool window.
        /// </summary>
        public void ShowToolWindow() => this.Package.ShowToolWindow();

        /// <summary>
        /// Get Visual Studio service by type.
        /// </summary>
        /// <param name="serviceType">Needed service type.</param>
        /// <returns>Result VS service instance</returns>
        public async Task<object> GetServiceAsync(Type serviceType) => await this.serviceProvider.GetServiceAsync(serviceType);

        /// <summary>
        /// Get Visual Studio service by type (not async method).
        /// </summary>
        /// <param name="serviceType">Needed service type.</param>
        /// <returns>Result VS service instance</returns>
        public object GetService(Type serviceType) => null;

        /// <summary>
        /// Initialize service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token instance.</param>
        /// <returns>Task.</returns>
        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            IVsActivityLog activityLog = await this.serviceProvider.GetServiceAsync(typeof(SVsActivityLog)) as IVsActivityLog;

            this.activityLogger = new SnykActivityLogger(activityLog);

            this.activityLogger.LogInformation("Initialize Snyk services");

            await this.Package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            this.settingsManager = new ShellSettingsManager(this.Package);

            this.vsThemeService = new SnykVsThemeService(this);
            await this.vsThemeService.InitializeAsync();

            await SnykToolWindowCommand.InitializeAsync(this);

            await SnykTasksService.InitializeAsync(this);

            await SnykSolutionService.InitializeAsync(this);

            this.dte = await this.serviceProvider.GetServiceAsync(typeof(DTE)) as DTE;

            this.tasksService = SnykTasksService.Instance;
            this.solutionService = SnykSolutionService.Instance;

            NotificationService.Initialize(this);

            VsStatusBar.Initialize(this);

            this.activityLogger.LogInformation("Leave SnykService.InitializeAsync");
        }

        /// <summary>
        /// Create new instance of SnykCli class with Options and Logger parameters.
        /// </summary>
        /// <returns>New SnykCli instance.</returns>
        public SnykCli NewCli() => new SnykCli
        {
            Options = this.Options,
            Logger = this.ActivityLogger,
        };

        /// <summary>
        /// Check is Options.ApiToken initialized. But if it's empty it will call CLI.GetApiToken() method.
        /// </summary>
        /// <returns>User API token string</returns>
        public string GetApiToken()
        {
            if (!string.IsNullOrEmpty(this.Options.ApiToken))
            {
                return this.Options.ApiToken;
            }

            return this.NewCli().GetApiToken();
        }
    }
}
