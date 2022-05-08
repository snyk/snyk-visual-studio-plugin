using Snyk.Analytics;

namespace Snyk.VisualStudio.Extension.Shared
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using Microsoft.VisualStudio.Shell;
    using Serilog;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.Commands;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using Snyk.VisualStudio.Extension.Shared.Settings;
    using Snyk.VisualStudio.Extension.Shared.UI.Notifications;
    using Snyk.VisualStudio.Extension.Shared.UI.Toolwindow;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    /// [PackageRegistration(UseManagedResourcesOnly = true)]    
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(SnykVSPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideService(typeof(ISnykService), IsAsyncQueryable = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(SnykToolWindow), Style = VsDockStyle.Tabbed)]
    [ProvideOptionPage(typeof(SnykGeneralOptionsDialogPage), "Snyk", "General settings", 1000, 1001, true)]
    [ProvideOptionPage(typeof(SnykSolutionOptionsDialogPage), "Snyk", "Solution settings", 1000, 1002, true)]
    public sealed class SnykVSPackage : AsyncPackage
    {
        /// <summary>
        /// SnykVSPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "5ddf9abb-42ec-49b9-b201-b3e2fc2f8f89";

        private static readonly ILogger Logger = LogManager.ForContext<SnykVSPackage>();

        private static SnykVSPackage instance;

        private SnykGeneralOptionsDialogPage generalOptionsDialogPage;

        private ISnykServiceProvider serviceProvider;

        private SnykToolWindow toolWindow;

        private SnykToolWindowControl toolWindowControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykVSPackage"/> class.
        /// </summary>
        public SnykVSPackage() => instance = this;

        /// <summary>
        /// Gets a value indicating whether ServiceProvider.
        /// </summary>
        public static ISnykServiceProvider ServiceProvider => instance.serviceProvider;

        /// <summary>
        /// Gets a value indicating whether ToolWindow Control.
        /// </summary>
        public SnykToolWindowControl ToolWindowControl => this.toolWindowControl;

        /// <summary>
        /// Gets a value indicating whether general Options dialog.
        /// </summary>
        public SnykGeneralOptionsDialogPage GeneralOptionsDialogPage => this.generalOptionsDialogPage;

        /// <summary>
        /// Gets <see cref="SnykToolWindow"/> instance.
        /// </summary>
        public SnykToolWindow ToolWindow => this.toolWindow;

        /// <summary>
        /// Show Options dialog.
        /// </summary>
        public void ShowOptionPage() => this.ShowOptionPage(typeof(SnykGeneralOptionsDialogPage));

        /// <summary>
        /// Create <see cref="SnykService"/> object.
        /// </summary>
        /// <param name="container">Container.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="serviceType">Service type.</param>
        /// <returns>Task.</returns>
        public async Task<object> CreateSnykServiceAsync(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
        {
            var service = new SnykService(this);

            await service.InitializeAsync(cancellationToken);

            return service;
        }

        /// <summary>
        /// Initialize tool window.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task InitializeToolWindowAsync()
        {
            if (this.toolWindow == null)
            {
                Logger.Information("ToolWindow is not initialized. Call await JoinableTaskFactory.SwitchToMainThreadAsync().");

                await this.JoinableTaskFactory.SwitchToMainThreadAsync();

                Logger.Information("Call FindToolWindow().");

                this.toolWindow = this.FindToolWindow(typeof(SnykToolWindow), 0, true) as SnykToolWindow;

                Logger.Information($"Check ToolWindow is not null {this.toolWindow}.");

                if (this.toolWindow == null || this.toolWindow.Frame == null)
                {
                    Logger.Error("Exception: Cannot find Snyk tool window.");

                    throw new NotSupportedException("Cannot find Snyk tool window.");
                }

                Logger.Information("Initialize ToolWindow.Content. Call await JoinableTaskFactory.SwitchToMainThreadAsync().");

                await this.JoinableTaskFactory.SwitchToMainThreadAsync();

                Logger.Information("Call ToolWindow.Content.");

                this.toolWindowControl = (SnykToolWindowControl)this.toolWindow.Content;

                Logger.Information("Leave InitializeToolWindowControlAsync() method");
            }
        }

        /// <summary>
        /// Initialize package.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="progress">Progress.</param>
        /// <returns>Task.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            try
            {
                await base.InitializeAsync(cancellationToken, progress);

                this.AddService(typeof(SnykService), this.CreateSnykServiceAsync, true);

                this.serviceProvider = await this.GetServiceAsync(typeof(SnykService)) as SnykService ?? throw new InvalidOperationException("Could not find Snyk Service");

                Logger.Information("Get SnykService as ServiceProvider.");
                Logger.Information("Start InitializeGeneralOptionsAsync.");

                await this.InitializeGeneralOptionsAsync();

                // Initialize analytics
                await this.serviceProvider.AnalyticsService.ObtainUserAsync(this.serviceProvider.GetApiToken());
                await this.serviceProvider.SentryService.SetupAsync();

                // Initialize commands
                Logger.Information("Initialize Commands()");
                await SnykScanCommand.InitializeAsync(this);
                await SnykStopCurrentTaskCommand.InitializeAsync(this);
                await SnykCleanPanelCommand.InitializeAsync(this);
                await SnykOpenSettingsCommand.InitializeAsync(this);

                // Initialize tool-window
                Logger.Information("Initializing tool window");
                await this.InitializeToolWindowAsync();
                VsStatusBarNotificationService.Instance.InitializeEventListeners(this.serviceProvider);
                Logger.Information("Before call toolWindowControl.InitializeEventListeners() method.");
                this.toolWindowControl.InitializeEventListeners(this.serviceProvider);
                this.toolWindowControl.Initialize(this.serviceProvider);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error on intialize Snyk VS package");
            }
        }

        /// <summary>
        /// Dispose analytics service and package.
        /// </summary>
        /// <param name="disposing">Bool.</param>
        protected override void Dispose(bool disposing)
        {
            this.serviceProvider.AnalyticsService?.Dispose();

            base.Dispose(disposing);
        }

        private async Task InitializeGeneralOptionsAsync()
        {
            if (this.generalOptionsDialogPage == null)
            {
                Logger.Information("Call GetDialogPage to create. await JoinableTaskFactory.SwitchToMainThreadAsync().");

                await this.JoinableTaskFactory.SwitchToMainThreadAsync();

                Logger.Information("GeneralOptionsDialogPage not created yet. Call GetDialogPage to create.");

                this.generalOptionsDialogPage = (SnykGeneralOptionsDialogPage)this.GetDialogPage(typeof(SnykGeneralOptionsDialogPage));

                this.generalOptionsDialogPage.LoadSettingsFromStorage();

                Logger.Information("Call generalOptionsDialogPage.Initialize()");

                this.generalOptionsDialogPage.Initialize(serviceProvider);
            }
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        #endregion
    }
}