using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using Snyk.VisualStudio.Extension.Commands;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.UI.Toolwindow;
using Task = System.Threading.Tasks.Task;

namespace Snyk.VisualStudio.Extension
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

        public static SnykVSPackage Instance;
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private ISnykServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykVSPackage"/> class.
        /// </summary>
        public SnykVSPackage()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets a value indicating whether ServiceProvider.
        /// </summary>
        public static ISnykServiceProvider ServiceProvider => Instance.serviceProvider;

        // Used only in tests
        public void SetServiceProvider(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }
        /// <summary>
        /// Gets a task that completes once the Snyk extension has been initialized.
        /// </summary>
        public static Task PackageInitializedAwaiter => initializationTaskCompletionSource.Task;

        /// <summary>
        /// Gets a value indicating whether ToolWindow Control.
        /// </summary>
        public SnykToolWindowControl ToolWindowControl { get; set; }

        // <summary>
        /// Gets the Options
        /// </summary>
        public ISnykOptions Options { get; private set; }
        public ISnykGeneralOptionsDialogPage SnykGeneralOptionsDialogPage { get; private set; }

        /// <summary>
        /// Gets <see cref="SnykToolWindow"/> instance.
        /// </summary>
        public SnykToolWindow ToolWindow { get; set; }

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
            var ideVersion = await GetVsMajorMinorVersionAsync();
            var service = new SnykService(this, ideVersion);

            await service.InitializeAsync(cancellationToken);

            return service;
        }

        /// <summary>
        /// Initialize tool window.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task EnsureInitializeToolWindowAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            if (ToolWindow != null) return;
            Logger.Information("Call FindToolWindow().");
            ToolWindow = FindToolWindow(typeof(SnykToolWindow), 0, true) as SnykToolWindow;

            Logger.Information("Check ToolWindow is not null {ToolWindow}.", ToolWindow);

            if (ToolWindow == null || ToolWindow.Frame == null)
            {
                Logger.Error("Exception: Cannot find Snyk tool window.");

                throw new NotSupportedException("Cannot find Snyk tool window.");
            }

            Logger.Information("Call ToolWindow.Content.");
            ToolWindowControl = (SnykToolWindowControl)ToolWindow.Content;
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
                await SetLanguageClientManagerAsync();

                Logger.Information("Get SnykService as ServiceProvider.");
                Logger.Information("Start InitializeGeneralOptionsAsync.");
                
                await InitializeGeneralOptionsAsync();


                // Initialize LS
                Logger.Information("Initializing Language Server");
                InitializeLanguageClientAsync().FireAndForget();

                // Initialize commands
                Logger.Information("Initialize Commands()");

                await SnykScanCommand.InitializeAsync(this);
                await SnykStopCurrentTaskCommand.InitializeAsync(this);
                await SnykCleanPanelCommand.InitializeAsync(this);
                await SnykOpenSettingsCommand.InitializeAsync(this);
            
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
            base.Dispose(disposing);
        }

        private async Task SetLanguageClientManagerAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.SatisfyImportsOnce();
            var componentModel = GetGlobalService(typeof(SComponentModel)) as IComponentModel;
            Assumes.Present(componentModel);
            var languageServerClientManager = componentModel.GetService<ILanguageClientManager>();
            this.serviceProvider.LanguageClientManager = languageServerClientManager;
        }
        
        private async Task InitializeLanguageClientAsync()
        {
            try
            {
                this.serviceProvider.LanguageClientManager.OnLanguageClientNotInitializedAsync += LanguageClientManagerOnLanguageClientNotInitializedAsync;
                this.serviceProvider.LanguageClientManager.OnLanguageServerReadyAsync += LanguageClientManagerOnOnLanguageServerReadyAsync;
                if (!LanguageClientHelper.IsLanguageServerReady())
                {
                    // If CLI download is necessary, Skip initializing.
                    if (this.serviceProvider.TasksService.ShouldDownloadCli())
                    {
                        return;
                    }
                    await this.serviceProvider.LanguageClientManager.StartServerAsync(true);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, string.Empty);
            }
        }

        private async Task LanguageClientManagerOnOnLanguageServerReadyAsync(object sender, SnykLanguageServerEventArgs args)
        {
            this.serviceProvider.FeatureFlagService.RefreshAsync(DisposalToken).FireAndForget();
            // Sleep for three seconds before closing the temp window
            await Task.Delay(1000);
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            tempOpenedFileWindow?.Close(vsSaveChanges.vsSaveChangesNo);
        }

        private Window tempOpenedFileWindow;
        private async Task LanguageClientManagerOnLanguageClientNotInitializedAsync(object sender, SnykLanguageServerEventArgs args)
        {
            await semaphore.WaitAsync(DisposalToken);

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync();
                    while (!LanguageClientHelper.IsLanguageServerReady())
                    {
                        var isSolutionOrFolderOpen = SnykSolutionService.Instance.IsSolutionOpen();
                        if (isSolutionOrFolderOpen)
                        {
                            var dte = (DTE)await GetServiceAsync(typeof(DTE));
                            if (dte == null) return;

                            // Get the path to the file within the installed extension directory
                            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                            if (assemblyLocation == null) return;

                            var filePath = Path.Combine(assemblyLocation, "Resources", "SnykLsInit.cs");

                            // Open the file
                            tempOpenedFileWindow =
                                dte.ItemOperations.OpenFile(filePath, EnvDTE.Constants.vsViewKindTextView);
                            return;
                        }

                        await Task.Delay(3000, DisposalToken);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("LanguageClientManagerOnLanguageClientNotInitializedAsync Failed with {Ex}", ex);
                }
                finally
                {
                    semaphore.Release();
                }
            }).FireAndForget();
        }

        private async Task InitializeGeneralOptionsAsync()
        {
            if (Options == null)
            {
                Logger.Information(
                    "Call GetDialogPage to create. await JoinableTaskFactory.SwitchToMainThreadAsync().");
                Options = new SnykOptions(this.serviceProvider);
                var readableVsVersion = await this.GetReadableVsVersionAsync();
                var vsMajorMinorVersion = await this.GetVsMajorMinorVersionAsync();
                Options.Application = readableVsVersion;
                Options.ApplicationVersion = vsMajorMinorVersion;
                Options.IntegrationEnvironment = readableVsVersion;
                Options.IntegrationEnvironmentVersion = vsMajorMinorVersion;
            }

            if (SnykGeneralOptionsDialogPage == null)
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();

                Logger.Information("GeneralOptionsDialogPage not created yet. Call GetDialogPage to create.");

                SnykGeneralOptionsDialogPage =
                    (SnykGeneralOptionsDialogPage)GetDialogPage(typeof(SnykGeneralOptionsDialogPage));
                Logger.Information("Call generalOptionsDialogPage.Initialize()");
                SnykGeneralOptionsDialogPage.Initialize(this.serviceProvider);
            }
        }

        private async Task<string> GetVsVersionAsync()
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

        private async Task<string> GetVsMajorMinorVersionAsync()
        {
            try
            {
                var vsVersionString = (await GetVsVersionAsync()).Split('.');

                return $"{vsVersionString[0]}.{vsVersionString[1]}";
            }
            catch
            {
                return "0.0";
            }
        }

        private async Task<string> GetReadableVsVersionAsync()
        {
            try
            {
                var vsVersionString = await GetVsVersionAsync();
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