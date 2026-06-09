using System;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.UI.Html;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// WebView2-backed panel that renders the Language Server's HTML issue tree
    /// (the <c>$/snyk.treeView</c> payload). Replaces the former native WPF tree.
    /// Tree → IDE interactions (navigate, filter, expand/collapse, error details) flow
    /// through the shared <see cref="ExecuteCommandBridge"/> <c>window.__ideExecuteCommand__</c>
    /// contract to the Language Server via <c>workspace/executeCommand</c>.
    /// </summary>
    public partial class TreeHtmlPanel : UserControl, IDisposable
    {
        private static readonly ILogger Logger = LogManager.ForContext<TreeHtmlPanel>();

        private readonly WebView2Host host;
        private readonly IHtmlProvider htmlProvider = TreeHtmlProvider.Instance;
        private bool _disposed;

        /// <summary>
        /// Total issue count from the last <c>$/snyk.treeView</c> notification. Drives the
        /// "Clean" command's enabled state (see <c>SnykToolWindowControl.IsTreeContentNotEmpty</c>).
        /// </summary>
        public int TotalIssues { get; set; }

        /// <summary>
        /// Whether to expose Chromium DevTools (F12) on the hosted WebView2. Gated on DEBUG so
        /// Release builds never ship DevTools while local developer builds get the JS console.
        /// </summary>
        private static bool DeveloperToolsEnabled =>
#if DEBUG
            true;
#else
            false;
#endif

        public TreeHtmlPanel()
        {
            this.InitializeComponent();

            var linkOpener = new SnykScriptManager(SnykVSPackage.ServiceProvider);

            // The tree HTML calls window.__ideExecuteCommand__(command, args, callback). The
            // BuildClientScript shim (registered as an init script below) stashes the JS callback
            // and posts { method:'__ideExecuteCommand__', args:[command, argsJson, callbackId] }.
            var dispatcher = new WebView2MessageDispatcher()
                .Register("__ideExecuteCommand__", 3, args => ExecuteCommandFromBridge(
                    args[0].Value<string>(),
                    args[1].Value<string>(),
                    args[2].Value<string>()))
                .Register("OpenLink", 1, args => linkOpener.OpenLink(args[0].Value<string>()));

            // Shared with the description + summary panels — all three live in the same tool window
            // and can safely share one browser process via a common user-data folder.
            var userDataFolder = WebView2Host.BuildUserDataFolder("toolwindow");

            host = new WebView2Host(
                TreeHtmlViewer,
                dispatcher,
                userDataFolder,
                additionalInitScripts: new[]
                {
                    ExecuteCommandBridge.BuildClientScript(),
                },
                enableDeveloperTools: DeveloperToolsEnabled);

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await host.InitializeAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "WebView2 initialization failed in tree panel");
                }
            }).FireAndForget();
        }

        /// <summary>
        /// Renders the LS-provided tree HTML. Called from the <c>$/snyk.treeView</c> notification
        /// handler and from the initial <c>snyk.getTreeView</c> fetch.
        /// </summary>
        public void SetContent(string html)
        {
            if (string.IsNullOrEmpty(html)) return;

            var themedHtml = htmlProvider.ReplaceCssVariables(html);

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await host.NavigateAsync(themedHtml);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to navigate tree panel");
                }
            }).FireAndForget();
        }

        /// <summary>
        /// Shows a lightweight placeholder until the first tree HTML arrives, then asks the LS for
        /// the current tree (so the panel is populated even before the first scan-state change).
        /// </summary>
        public void Init()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await host.NavigateAsync("<html><body style='margin:0;padding:0;'>Loading...</body></html>");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to initialize tree panel content");
                }
            }).FireAndForget();
        }

        /// <summary>
        /// Fetches the current tree HTML from the Language Server via <c>snyk.getTreeView</c> and
        /// renders it. Safe to call once the LS is ready; the LS also pushes updates via
        /// <c>$/snyk.treeView</c> on every scan-state change.
        /// </summary>
        public void RequestInitialTree()
        {
            var languageClientManager = LanguageClientHelper.LanguageClientManager();
            if (languageClientManager == null) return;

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    var result = await languageClientManager.InvokeExecuteCommandAsync(
                        LsConstants.SnykGetTreeView, Array.Empty<object>(), SnykVSPackage.Instance.DisposalToken);
                    if (result != null)
                    {
                        SetContent(result.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to fetch initial tree view from Language Server");
                }
            }).FireAndForget();
        }

        private void ExecuteCommandFromBridge(string command, string argsJson, string callbackId)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ExecuteCommandBridge.DispatchAsync(
                    SnykVSPackage.ServiceProvider.LanguageClientManager,
                    command,
                    argsJson,
                    callbackId,
                    InvokeCommandCallback,
                    SnykVSPackage.Instance.DisposalToken);
            }).FireAndForget();
        }

        private void InvokeCommandCallback(string callbackId, string resultJson)
        {
            if (!ExecuteCommandBridge.IsValidCallbackId(callbackId))
            {
                Logger.Warning("Rejected callbackId with unexpected format: {CallbackId}", callbackId);
                return;
            }

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
                    await host.ExecuteScriptAsync(
                        ExecuteCommandBridge.BuildCommandCallbackScript(callbackId, resultJson));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error invoking tree command callback {CallbackId}", callbackId);
                }
            }).FireAndForget();
        }

        // Disposes the underlying WebView2Host, which unsubscribes from WebMessageReceived and
        // sweeps any oversized-HTML temp files. Called by SnykToolWindowControl.Dispose.
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            host?.Dispose();
        }
    }
}
