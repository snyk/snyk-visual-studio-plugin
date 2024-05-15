using Snyk.Common.Settings;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using Snyk.Common;
using Snyk.VisualStudio.Extension.Shared.Commands;
using Snyk.VisualStudio.Extension.Shared.Service;
using Snyk.VisualStudio.Extension.Shared.Settings;
using Snyk.VisualStudio.Extension.Shared.UI.Notifications;
using Snyk.VisualStudio.Extension.Shared.UI.Toolwindow;
using Task = System.Threading.Tasks.Task;

namespace Snyk.VisualStudio.Extension.Shared
{
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
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in
    /// .vsixmanifest file.
    /// </para>
    /// </remarks>
    /// [PackageRegistration(UseManagedResourcesOnly = true)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules",
        "SA1650:ElementDocumentationMustBeSpelledCorrectly",
        Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideService(typeof(ISnykService), IsAsyncQueryable = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(SnykToolWindow), Style = VsDockStyle.Tabbed)]
    [ProvideOptionPage(typeof(SnykGeneralOptionsDialogPage), "Snyk", "General settings", 1000, 1001, true)]
    [ProvideOptionPage(typeof(SnykSolutionOptionsDialogPage), "Snyk", "Solution settings", 1000, 1002, true)]
    public sealed class SnykVSPackage : AsyncPackage, ISnykOptionsProvider
    {
        /// <summary>
        /// SnykVSPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "5ddf9abb-42ec-49b9-b201-b3e2fc2f8f89";

        private static readonly ILogger Logger = LogManager.ForContext<SnykVSPackage>();

        private static readonly TaskCompletionSource<bool> initializationTaskCompletionSource =
            new TaskCompletionSource<bool>();

        private static SnykVSPackage instance;

        private ISnykServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykVSPackage"/> class.
        /// </summary>
        public SnykVSPackage()
        {
            instance = this;
        }

        /// <summary>
        /// Gets a value indicating whether ServiceProvider.
        /// </summary>
        public static ISnykServiceProvider ServiceProvider => instance.serviceProvider;

        /// <summary>
        /// Gets a task that completes once the Snyk extension has been initialized.
        /// </summary>
        public static Task PackageInitializedAwaiter => initializationTaskCompletionSource.Task;

        /// <summary>
        /// Gets a value indicating whether ToolWindow Control.
        /// </summary>
        public SnykToolWindowControl ToolWindowControl { get; private set; }

        /// <summary>
        /// Gets a value indicating whether general Options dialog.
        /// </summary>
        public SnykGeneralOptionsDialogPage GeneralOptionsDialogPage { get; private set; }
        
        // <summary>
        /// Gets the Options
        /// </summary>
        public ISnykOptions Options => GeneralOptionsDialogPage;

        /// <summary>
        /// Gets <see cref="SnykToolWindow"/> instance.
        /// </summary>
        public SnykToolWindow ToolWindow { get; private set; }

        /// <summary>
        /// True if the package was initialized successfully.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Show Options dialog.
        /// </summary>
        public void ShowOptionPage() => ShowOptionPage(typeof(SnykGeneralOptionsDialogPage));

        /// <summary>
        /// Create <see cref="SnykService"/> object.
        /// </summary>
        /// <param name="container">Container.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="serviceType">Service type.</param>
        /// <returns>Task.</returns>
        public async Task<object> CreateSnykServiceAsync(IAsyncServiceContainer container,
            CancellationToken cancellationToken, Type serviceType)
        {
            var ideVersion = await GetVsMajorMinorVersion();
            var service = new SnykService(this, ideVersion);

            await service.InitializeAsync(cancellationToken);

            return service;
        }

        /// <summary>
        /// Initialize tool window.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task InitializeToolWindowAsync()
        {
            if (ToolWindow == null)
            {
                Logger.Information(
                    "ToolWindow is not initialized. Call await JoinableTaskFactory.SwitchToMainThreadAsync().");

                await JoinableTaskFactory.SwitchToMainThreadAsync();

                Logger.Information("Call FindToolWindow().");

                ToolWindow = FindToolWindow(typeof(SnykToolWindow), 0, true) as SnykToolWindow;

                Logger.Information($"Check ToolWindow is not null {ToolWindow}.");

                if (ToolWindow == null || ToolWindow.Frame == null)
                {
                    Logger.Error("Exception: Cannot find Snyk tool window.");

                    throw new NotSupportedException("Cannot find Snyk tool window.");
                }

                Logger.Information(
                    "Initialize ToolWindow.Content. Call await JoinableTaskFactory.SwitchToMainThreadAsync().");

                await JoinableTaskFactory.SwitchToMainThreadAsync();

                Logger.Information("Call ToolWindow.Content.");

                ToolWindowControl = (SnykToolWindowControl) ToolWindow.Content;

                Logger.Information("Leave InitializeToolWindowControlAsync() method");
            }
        }

        /// <summary>
        /// Initialize package.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="progress">Progress.</param>
        /// <returns>Task.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            try
            {
                await base.InitializeAsync(cancellationToken, progress);

                AddService(typeof(SnykService), CreateSnykServiceAsync, true);

                this.serviceProvider = await GetServiceAsync(typeof(SnykService)) as SnykService ??
                                       throw new InvalidOperationException("Could not find Snyk Service");

                Logger.Information("Get SnykService as ServiceProvider.");
                Logger.Information("Start InitializeGeneralOptionsAsync.");
                
                await InitializeGeneralOptionsAsync();

                // Initialize analytics
                var vsVersion = await GetReadableVsVersion();

                var tokenString = this.serviceProvider.NewCli().GetApiToken();
                this.serviceProvider.Options.SetApiToken(tokenString);
                var token = this.serviceProvider.Options.ApiToken;

                await this.serviceProvider.AnalyticsService.ObtainUserAsync(token, vsVersion);
                await this.serviceProvider.SentryService.SetupAsync();

                // Initialize commands
                Logger.Information("Initialize Commands()");
                await SnykScanCommand.InitializeAsync(this);
                await SnykStopCurrentTaskCommand.InitializeAsync(this);
                await SnykCleanPanelCommand.InitializeAsync(this);
                await SnykOpenSettingsCommand.InitializeAsync(this);

                // Initialize tool-window
                Logger.Information("Initializing tool window");
                await InitializeToolWindowAsync();
                VsStatusBarNotificationService.Instance.InitializeEventListeners(this.serviceProvider);
                Logger.Information("Before call toolWindowControl.InitializeEventListeners() method.");
                ToolWindowControl.InitializeEventListeners(this.serviceProvider);
                ToolWindowControl.Initialize(this.serviceProvider);

                // Notify package has been initialized
                IsInitialized = true;
                initializationTaskCompletionSource.SetResult(true);
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
            if (GeneralOptionsDialogPage == null)
            {
                Logger.Information(
                    "Call GetDialogPage to create. await JoinableTaskFactory.SwitchToMainThreadAsync().");

                await JoinableTaskFactory.SwitchToMainThreadAsync();

                Logger.Information("GeneralOptionsDialogPage not created yet. Call GetDialogPage to create.");

                GeneralOptionsDialogPage =
                    (SnykGeneralOptionsDialogPage) GetDialogPage(typeof(SnykGeneralOptionsDialogPage));

                GeneralOptionsDialogPage.LoadSettingsFromStorage();

                Logger.Information("Call generalOptionsDialogPage.Initialize()");

                GeneralOptionsDialogPage.Initialize(this.serviceProvider);
                var readableVsVersion = await this.GetReadableVsVersion();
                var vsMajorMinorVersion = await this.GetVsMajorMinorVersion();
                GeneralOptionsDialogPage.Application = readableVsVersion;
                GeneralOptionsDialogPage.ApplicationVersion = vsMajorMinorVersion;
                GeneralOptionsDialogPage.IntegrationEnvironment = readableVsVersion;
                GeneralOptionsDialogPage.IntegrationEnvironmentVersion = vsMajorMinorVersion;
            }
        }

        private async Task<string> GetVsVersion()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var shell = await this.GetServiceAsync(typeof(SVsShell)) as IVsShell;
                if (shell is null)
                {
                    return "0.0.0";
                }

                shell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out var vsVersion);
                return vsVersion as string ?? "0.0.0";
            }
            catch
            {
                return "0.0.0";
            }
        }

        private async Task<string> GetVsMajorMinorVersion()
        {
            try
            {
                var vsVersionString = (await GetVsVersion()).Split('.');

                return $"{vsVersionString[0]}.{vsVersionString[1]}";
            }
            catch
            {
                return "0.0";
            }
        }

        private async Task<string> GetReadableVsVersion()
        {
            try
            {
                var vsVersionString = await GetVsVersion();
                var major = vsVersionString.Split('.').FirstOrDefault();

                return major switch
                {
                    "17" => "Visual Studio 2022",
                    "16" => "Visual Studio 2019",
                    "15" => "Visual Studio 2017",
                    "14" => "Visual Studio 2015",
                    _ => "Unknown Visual Studio version"
                };
            }
            catch
            {
                return "Unknown Visual Studio version";
            }
        }
    }

    // Interface to enable testing with mocks
    public interface ISnykOptionsProvider
    {
        ISnykOptions Options { get; }
    }
}