namespace Snyk.VisualStudio.Extension
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using UI.Toolwindow;
    using Settings;
    using Service;
    using Microsoft.VisualStudio.Shell;
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
    [ProvideOptionPage(typeof(SnykProjectOptionsDialogPage), "Snyk", "Project settings", 1000, 1002, true)]
    public sealed class SnykVSPackage : AsyncPackage
    {
        /// <summary>
        /// SnykVSPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "5ddf9abb-42ec-49b9-b201-b3e2fc2f8f89";

        private SnykGeneralOptionsDialogPage generalOptionsDialogPage;

        private ISnykServiceProvider serviceProvider;

        private SnykToolWindow toolWindow;

        private SnykToolWindowControl toolWindowControl;

        private SnykActivityLogger logger;

        /// <summary>
        /// Gets a value indicating whether ServiceProvider.
        /// </summary>
        public ISnykServiceProvider ServiceProvider => this.serviceProvider;

        /// <summary>
        /// Gets a value indicating whether ToolWindow Control.
        /// </summary>
        public SnykToolWindowControl ToolWindowControl => this.toolWindowControl;

        /// <summary>
        /// Gets a value indicating whether general Options dialog.
        /// </summary>
        public SnykGeneralOptionsDialogPage GeneralOptionsDialogPage => this.generalOptionsDialogPage;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykVSPackage"/> class.
        /// </summary>
        public SnykVSPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

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
        /// Initialize package.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="progress">Progress.</param>
        /// <returns>Task.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            this.AddService(typeof(SnykService), this.CreateSnykServiceAsync, true);

            this.serviceProvider = await this.GetServiceAsync(typeof(SnykService)) as SnykService;

            this.logger = this.serviceProvider.ActivityLogger;

            this.logger.LogInformation("Get SnykService as ServiceProvider.");

            this.logger.LogInformation("Start InitializeGeneralOptionsAsync.");

            await this.InitializeGeneralOptionsAsync();

            new Task(() =>
            {
                this.serviceProvider.AnalyticsService.ObtainUser(this.serviceProvider.GetApiToken());
            }).Start();

            this.logger.LogInformation("Start Initialize tool window. Before call GetToolWindowControl() method.");

            await this.InitializeToolWindowAsync();

            logger.LogInformation("Before call toolWindowControl.InitializeEventListeners() method.");

            new Task(() =>
            {
                this.toolWindowControl.InitializeEventListeners(this.serviceProvider);
            }).Start();

            this.logger.LogInformation("Initialize Commands()");

            await Commands.SnykScanCommand.InitializeAsync(this);
            await Commands.SnykStopCurrentTaskCommand.InitializeAsync(this);
            await Commands.SnykCleanPanelCommand.InitializeAsync(this);
            await Commands.SnykOpenSettingsCommand.InitializeAsync(this);

            this.logger.LogInformation("Leave SnykVSPackage.InitializeAsync()");
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

        /// <summary>
        /// Initialize tool window.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task InitializeToolWindowAsync()
        {
            this.logger.LogInformation("Enter InitializeToolWindowAsync() method");

            if (this.toolWindow == null)
            {
                this.logger.LogInformation("ToolWindow is not initialized. Call await JoinableTaskFactory.SwitchToMainThreadAsync().");

                await this.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.logger.LogInformation("Call FindToolWindow().");

                this.toolWindow = this.FindToolWindow(typeof(SnykToolWindow), 0, true) as SnykToolWindow;

                this.logger.LogInformation($"Check ToolWindow is not null {this.toolWindow}.");

                if (this.toolWindow == null || this.toolWindow.Frame == null)
                {
                    this.logger.LogError("Exception: Cannot find Snyk tool window.");

                    throw new NotSupportedException("Cannot find Snyk tool window.");
                }

                this.logger.LogInformation("Initialize ToolWindow.Content. Call await JoinableTaskFactory.SwitchToMainThreadAsync().");

                await this.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.logger.LogInformation("Call ToolWindow.Content.");

                this.toolWindowControl = (SnykToolWindowControl)this.toolWindow.Content;

                this.logger.LogInformation("Leave InitializeToolWindowControlAsync() method");
            }

            this.logger.LogInformation("Leave InitializeToolWindowAsync() method");
        }

        /// <summary>
        /// Show tool window panel.
        /// </summary>
        public void ShowToolWindow() => this.toolWindowControl.ShowToolWindow();

        private async Task InitializeGeneralOptionsAsync()
        {
            this.serviceProvider.ActivityLogger.LogInformation("Enter InitializeGeneralOptionsAsync() method.");

            if (this.generalOptionsDialogPage == null)
            {
                this.serviceProvider.ActivityLogger.LogInformation("Call GetDialogPage to create. await JoinableTaskFactory.SwitchToMainThreadAsync().");

                await this.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.serviceProvider.ActivityLogger.LogInformation("GeneralOptionsDialogPage not created yet. Call GetDialogPage to create.");

                this.generalOptionsDialogPage = (SnykGeneralOptionsDialogPage)this.GetDialogPage(typeof(SnykGeneralOptionsDialogPage));

                this.generalOptionsDialogPage.LoadSettingsFromStorage();

                this.serviceProvider.ActivityLogger.LogInformation("Call generalOptionsDialogPage.Initialize()");

                this.generalOptionsDialogPage.Initialize(serviceProvider);
            }

            this.serviceProvider.ActivityLogger.LogInformation("Leave InitializeGeneralOptionsAsync() method.");
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        #endregion
    }
}