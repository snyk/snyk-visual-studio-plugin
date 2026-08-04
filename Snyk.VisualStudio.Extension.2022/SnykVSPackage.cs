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
using Snyk.VisualStudio.Extension.UI.Html;
using Snyk.VisualStudio.Extension.UI.Notifications;
using Snyk.VisualStudio.Extension.UI.Toolwindow;
using Task = System.Threading.Tasks.Task;

namespace Snyk.VisualStudio.Extension
{
    /// <summary>
    /// Describes what the not-initialized handler should do when the language server is not yet ready.
    /// Used by the testable seam <see cref="SnykVSPackage.DecideNotInitializedActivation"/>.
    /// </summary>
    internal enum ActivationDecision
    {
        /// <summary>Open the temp init file to activate the ILanguageClient.</summary>
        Activate,

        /// <summary>Language server is already ready — nothing to do.</summary>
        NoOp,
    }

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
    [ProvideOptionPage(typeof(HtmlSettingsDialogPage), "Snyk", "General", 1000, 1001, true)]
    public sealed class SnykVSPackage : AsyncPackage
    {
        /// <summary>
        /// SnykVSPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "5ddf9abb-42ec-49b9-b201-b3e2fc2f8f89";

        private static readonly ILogger Logger = LogManager.ForContext<SnykVSPackage>();

        private static readonly TaskCompletionSource<bool> initializationTaskCompletionSource =
            new TaskCompletionSource<bool>();

        public static SnykVSPackage Instance;
        private readonly SemaphoreSlim languageClientInitSemaphore = new SemaphoreSlim(1, 1);

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

        public HtmlSettingsDialogPage HtmlSettingsDialogPage { get; private set; }

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
        public void ShowOptionPage() => ShowOptionPage(typeof(HtmlSettingsDialogPage));

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
                
                await InitializeOptionsAsync();


                // Initialize LS
                Logger.Information("Initializing Language Server");
                InitializeLanguageClient();

                // Initialize commands
                Logger.Information("Initialize Commands()");

                await SnykScanCommand.InitializeAsync(this);
                await SnykStopCurrentTaskCommand.InitializeAsync(this);
                await SnykCleanPanelCommand.InitializeAsync(this);
                await SnykOpenSettingsCommand.InitializeAsync(this);

                // The Edge WebView2 Runtime is a hard requirement now that the settings dialog and
                // all tool-window panels are WebView2-hosted. Surface a clear, actionable error if
                // it's missing rather than letting every panel fail opaquely on first navigation.
                await WarnIfWebView2RuntimeMissingAsync();

                // Notify package has been initialized
                IsInitialized = true;
                initializationTaskCompletionSource.SetResult(true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error on intialize Snyk VS package");
            }
        }

        // Checks for the Edge WebView2 Runtime and, if absent, logs and shows an InfoBar pointing
        // the user at the installer. Detection (GetAvailableBrowserVersionString) is thread-safe;
        // the InfoBar must be shown on the UI thread.
        private async Task WarnIfWebView2RuntimeMissingAsync()
        {
            if (WebView2Host.IsRuntimeAvailable())
                return;

            Logger.Error("Microsoft Edge WebView2 Runtime not found. Snyk settings and panels cannot render until it is installed.");

            await JoinableTaskFactory.SwitchToMainThreadAsync();
            NotificationService.Instance?.ShowErrorInfoBar(
                "Snyk requires the Microsoft Edge WebView2 Runtime to display its settings and panels, but it is not installed. " +
                "Install it from https://developer.microsoft.com/microsoft-edge/webview2/ and restart Visual Studio.");
        }

        /// <summary>
        /// Dispose analytics service and package.
        /// </summary>
        /// <param name="disposing">Bool.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && Options != null)
            {
                // Detach the LS-push handler so it can't fire (and touch the LanguageClientManager)
                // during or after package teardown.
                Options.SettingsChanged -= OnOptionsSettingsChanged;
            }

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
        
        private void InitializeLanguageClient()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    this.serviceProvider.LanguageClientManager.OnLanguageClientNotInitializedAsync += LanguageClientManagerOnLanguageClientNotInitializedAsync;
                    this.serviceProvider.LanguageClientManager.OnLanguageServerReadyAsync += LanguageClientManagerOnLanguageServerReadyAsync;
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
            }).FireAndForget();
        }

        private async Task LanguageClientManagerOnLanguageServerReadyAsync(object sender, SnykLanguageServerEventArgs args)
        {
            this.serviceProvider.FeatureFlagService.RefreshAsync(DisposalToken).FireAndForget();
            // Sleep for three seconds before closing the temp window
            await Task.Delay(1000);
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            tempOpenedFileWindow?.Close(vsSaveChanges.vsSaveChangesNo);
        }

        private Window tempOpenedFileWindow;

        /// <summary>
        /// Pure decision: given the current LS-ready state, decide whether to activate the
        /// ILanguageClient by opening the temp init file, or do nothing.
        ///
        /// Internal for testability — <c>InternalsVisibleTo("Snyk.VisualStudio.Extension.Tests")</c>
        /// is already declared in AssemblyInfo.cs.
        ///
        /// IDE-1752: solution state no longer gates activation. Activation is performed whenever
        /// the language server is not yet ready, regardless of whether a solution or folder is open.
        /// </summary>
        internal ActivationDecision DecideNotInitializedActivation(bool isLanguageServerReady)
        {
            if (isLanguageServerReady)
                return ActivationDecision.NoOp;

            return ActivationDecision.Activate;
        }

        /// <summary>
        /// Performs the activation side-effect (open the temp init file) using the provided action.
        /// The action defaults to the real DTE call; tests inject a spy.
        ///
        /// Internal for testability (IDE-1752 seam).
        /// Handles exceptions from the action: logs a single diagnostic and returns — no loop.
        /// </summary>
        internal async Task InvokeNotInitializedActivationAsync(Func<Task> openTempFileAction)
        {
            try
            {
                await openTempFileAction();
            }
            catch (OperationCanceledException)
            {
                // VS / LS client shutting down — not an error.
            }
            catch (Exception ex)
            {
                // S1: log a single clear diagnostic so a stuck state leaves a trail in the logs.
                // (Logger assertion in unit tests is intentionally absent — LogManager.ForContext<T>()
                // writes to a Lazy<Logger> file sink, not Serilog.Log.Logger; see brain note
                // log-manager-not-mockable.)
                Logger.Error(ex, "IDE-1752: Failed to open temp init file to activate the Snyk language client. " +
                    "The language server may not start. Check that the extension Resources/SnykLsInit.cs is present.");
            }
        }

        private async Task LanguageClientManagerOnLanguageClientNotInitializedAsync(object sender, SnykLanguageServerEventArgs args)
        {
            await languageClientInitSemaphore.WaitAsync(DisposalToken);

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync();

                    var decision = DecideNotInitializedActivation(
                        isLanguageServerReady: LanguageClientHelper.IsLanguageServerReady());

                    if (decision == ActivationDecision.NoOp)
                        return;

                    // Activate: open the bundled temp file to trigger VS ILanguageClient activation.
                    // IDE-1752 fix: this activation now runs for both the solution-open and the
                    // no-solution cases, removing the unbounded Task.Delay loop that caused the hang.
                    await InvokeNotInitializedActivationAsync(async () =>
                    {
                        await JoinableTaskFactory.SwitchToMainThreadAsync();
                        var dte = (DTE)await GetServiceAsync(typeof(DTE));
                        if (dte == null)
                        {
                            Logger.Error("IDE-1752: DTE service unavailable; cannot open temp init file to activate the Snyk language client.");
                            return;
                        }

                        var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        if (assemblyLocation == null)
                        {
                            Logger.Error("IDE-1752: Assembly location unavailable; cannot locate Resources/SnykLsInit.cs.");
                            return;
                        }

                        var filePath = Path.Combine(assemblyLocation, "Resources", "SnykLsInit.cs");

                        // Open the file — this activates the registered ILanguageClient (SnykLanguageClient
                        // is registered for the CSharp content type). OnLanguageServerReadyAsync will close
                        // this temp window once the LS is ready (S2).
                        tempOpenedFileWindow =
                            dte.ItemOperations.OpenFile(filePath, EnvDTE.Constants.vsViewKindTextView);
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error("LanguageClientManagerOnLanguageClientNotInitializedAsync Failed with {Ex}", ex);
                }
                finally
                {
                    languageClientInitSemaphore.Release();
                }
            }).FireAndForget();
        }

        private async Task InitializeOptionsAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            if (Options == null)
            {
                Options = serviceProvider.SnykOptionsManager.Load();
                var readableVsVersion = await this.GetReadableVsVersionAsync();
                var vsMajorMinorVersion = await this.GetVsMajorMinorVersionAsync();
                Options.Application = readableVsVersion;
                Options.ApplicationVersion = vsMajorMinorVersion;
                Options.IntegrationEnvironment = readableVsVersion;
                Options.IntegrationEnvironmentVersion = vsMajorMinorVersion;

                // Push IDE-side settings to the LS whenever they change. Previously wired in
                // SnykGeneralOptionsDialogPage.Initialize; relocated here when that DialogPage
                // was retired in favour of HtmlSettingsDialogPage. Unsubscribe-then-subscribe so
                // the handler is never wired twice if Options is ever reloaded/reassigned.
                Options.SettingsChanged -= OnOptionsSettingsChanged;
                Options.SettingsChanged += OnOptionsSettingsChanged;
            }

            if (HtmlSettingsDialogPage == null)
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();

                HtmlSettingsDialogPage =
                    (HtmlSettingsDialogPage)GetDialogPage(typeof(HtmlSettingsDialogPage));
                HtmlSettingsDialogPage.Initialize(this.serviceProvider);
            }
        }

        private void OnOptionsSettingsChanged(object sender, SnykSettingsChangedEventArgs e)
        {
            JoinableTaskFactory.RunAsync(async () =>
            {
                if (!LanguageClientHelper.IsLanguageServerReady())
                {
                    return;
                }

                try
                {
                    await this.serviceProvider.LanguageClientManager.DidChangeConfigurationAsync(DisposalToken);
                }
                catch (OperationCanceledException)
                {
                    // VS / LS client shutting down — not an error.
                }
                catch (Exception ex)
                {
                    // The save path reports success once settings are persisted to disk; pushing them
                    // to the Language Server happens here, after, and is otherwise fire-and-forget — so
                    // a failure would silently leave the running LS on the old settings while the user
                    // believes the change took effect. Surface it instead of swallowing it.
                    Logger.Error(ex, "Failed to push updated settings to the Language Server.");
                    NotificationService.Instance?.ShowErrorInfoBar(
                        "Snyk: your settings were saved but could not be sent to the Snyk Language Server. " +
                        "They will be applied the next time it starts. See the Snyk logs for details.");
                }
            }).FireAndForget();
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
}