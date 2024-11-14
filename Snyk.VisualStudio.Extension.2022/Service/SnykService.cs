﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Serilog;
using Snyk.Common;
using Snyk.Common.Settings;
using Snyk.VisualStudio.Extension.CLI;
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
    public class SnykService : ISnykServiceProvider, ISnykService
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykService>();

        private readonly IAsyncServiceProvider serviceProvider;
        private readonly string vsVersion;
        private SettingsManager settingsManager;

        private SnykVsThemeService vsThemeService;

        private SnykTasksService tasksService;

        private DTE2 dte;

        private SnykUserStorageSettingsService userStorageSettingsService;

        private IWorkspaceTrustService workspaceTrustService;
        
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
        public SnykToolWindowControl ToolWindow => this.Package.ToolWindowControl;

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

                Logger.Information("Leave SnykService.InitializeAsync");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error on initialize Snyk service");
            }
        }
    }
}