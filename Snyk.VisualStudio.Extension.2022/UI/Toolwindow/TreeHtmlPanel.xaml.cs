using System;
using System.Threading;
using System.Windows.Controls;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
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
    public partial class TreeHtmlPanel : UserControl, ITreeHtmlPanel, IDisposable
    {
        private static readonly ILogger Logger = LogManager.ForContext<TreeHtmlPanel>();

        private readonly IWebView2Host host;
        private readonly IHtmlProvider htmlProvider = TreeHtmlProvider.Instance;

        // Cancelled in Dispose so in-flight LS replies / bridge callbacks abandon their main-thread
        // continuations instead of touching the disposed host.
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        // Captured once from cts.Token immediately after construction, before any lambda closes over
        // it. On .NET Framework 4.8, reading CancellationTokenSource.Token after Dispose() throws
        // ObjectDisposedException. A CancellationToken struct captured before disposal stays safe to
        // read after the source is disposed: IsCancellationRequested returns true, and passing it to
        // SwitchToMainThreadAsync / InvokeExecuteCommandAsync yields the expected OperationCanceledException.
        private readonly CancellationToken ctsToken;

        // Monotonic navigation token. SetContent can be driven by both the $/snyk.treeView push and
        // the snyk.getTreeView pull (and re-fires of OnLanguageServerReadyAsync), which race; a
        // navigation whose generation has been superseded by a newer one is dropped so a stale tree
        // can't win the last NavigateAsync.
        private long navGeneration;

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
            ctsToken = cts.Token;
            this.InitializeComponent();

            // Theme the WPF placeholder (shown while the WebView2 is collapsed) and the WebView2's
            // post-init surface so nothing reads as a mismatched dark block. The WebView2 stays
            // collapsed until the first tree HTML arrives (see SetContent), so its dark
            // pre-initialization surface is never shown.
            var bgColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
            var textColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);
            RootGrid.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(bgColor.A, bgColor.R, bgColor.G, bgColor.B));
            LoadingText.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(textColor.A, textColor.R, textColor.G, textColor.B));
            TreeHtmlViewer.DefaultBackgroundColor = bgColor;

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
                    // host.InitializeAsync drives WebView2/CoreWebView2 setup, which must run on the
                    // UI thread. RunAsync alone doesn't guarantee the continuation resumes there, so
                    // switch explicitly — matching SetContent/InvokeCommandCallback below.
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    await host.InitializeAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "WebView2 initialization failed in tree panel");
                }
            }).FireAndForget();
        }

        /// <summary>
        /// Renders the LS-provided tree HTML and records the total issue count in one update.
        /// Called from the <c>$/snyk.treeView</c> notification handler (push) and from the initial
        /// <c>snyk.getTreeView</c> fetch (pull). HTML and count move together so a stale empty tree
        /// can't be pinned while the count reflects a newer scan.
        /// </summary>
        public void SetContent(string html, int totalIssues)
        {
            if (_disposed || string.IsNullOrEmpty(html)) return;

            // Increment synchronously so a later call's generation supersedes this one before
            // either lambda reaches the UI thread.
            var generation = Interlocked.Increment(ref navGeneration);

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ctsToken);
                    if (_disposed) return;

                    // Drop this navigation if a newer SetContent started after us — that one carries
                    // the more recent tree and must win. host.NavigateAsync awaits host.Ready
                    // internally; on first-time WebView2 init that await yields the UI thread, so a
                    // newer SetContent could navigate in that window and this older call would then
                    // resume and overwrite it with stale HTML. Await readiness here and re-check the
                    // generation AFTER it — the only yield before the actual NavigateToString — so the
                    // staleness guard, the count, and the navigation all commit together with no gap.
                    await host.Ready;
                    if (_disposed || Interlocked.Read(ref navGeneration) != generation) return;

                    // Both TotalIssues and ReplaceCssVariables run on the UI thread so they are
                    // always consistent with the navigation that wins the staleness race:
                    // a superseded SetContent never commits its count or resolves theme colours.
                    // TotalIssues is UI-thread-confined: all reads (IsTreeContentNotEmpty) and
                    // writes (Clean, here) switch to the UI thread before touching it.
                    TotalIssues = totalIssues;
                    var themedHtml = htmlProvider.ReplaceCssVariables(html);

                    // Reveal the WebView2 now that real content is ready and hide the WPF
                    // placeholder. After the first reveal these are no-ops; re-renders paint the
                    // themed DefaultBackgroundColor between navigations rather than the dark surface.
                    TreeHtmlViewer.Visibility = System.Windows.Visibility.Visible;
                    LoadingText.Visibility = System.Windows.Visibility.Collapsed;

                    await host.NavigateAsync(themedHtml);
                }
                catch (OperationCanceledException)
                {
                    // Disposed mid-flight; nothing to render.
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to navigate tree panel");
                }
            }).FireAndForget();
        }

        /// <summary>
        /// Clears the panel immediately and invalidates any in-flight <see cref="SetContent"/>
        /// navigations so a queued push cannot resurrect a tree that the user or
        /// <c>SnykToolWindowControl.Clean()</c> just cleared.
        /// <para>
        /// Bumping <c>navGeneration</c> causes any lambda already queued by <see cref="SetContent"/>
        /// to fail the <c>Interlocked.Read(ref navGeneration) != generation</c> staleness check on
        /// the UI thread and drop silently, preventing the cleared panel from being re-populated by
        /// a stale LS push or pull result.
        /// </para>
        /// Must be called on the UI thread (same requirement as <see cref="SetContent"/>).
        /// </summary>
        public void Reset()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_disposed) return;

            // Advance the generation so any SetContent lambda already queued (but not yet committed
            // to the UI thread) sees a generation mismatch and discards itself.
            Interlocked.Increment(ref navGeneration);

            TotalIssues = 0;

            // Return the panel to its initial placeholder state: collapse the WebView2 viewer and
            // restore the loading/placeholder text — mirroring what SetContent does in reverse.
            TreeHtmlViewer.Visibility = System.Windows.Visibility.Collapsed;
            LoadingText.Visibility = System.Windows.Visibility.Visible;
        }

        /// <summary>
        /// Fetches the current tree HTML from the Language Server via <c>snyk.getTreeView</c> and
        /// renders it. Safe to call once the LS is ready; the LS also pushes updates via
        /// <c>$/snyk.treeView</c> on every scan-state change.
        /// </summary>
        public void RequestInitialTree()
        {
            if (_disposed) return;

            var languageClientManager = LanguageClientHelper.LanguageClientManager();
            if (languageClientManager == null) return;

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    var result = await languageClientManager.InvokeExecuteCommandAsync(
                        LsConstants.SnykGetTreeView, Array.Empty<object>(), ctsToken);
                    if (_disposed) return;

                    // The reply uses the same envelope as the push ({ treeViewHtml, totalIssues }),
                    // so parse it as TreeViewParams. Calling result.ToString() would feed the raw
                    // JSON serialisation to the WebView2 as literal page text.
                    var treeViewParam = (result as JToken)?.ToObject<TreeViewParams>();
                    if (treeViewParam?.TreeViewHtml != null)
                    {
                        SetContent(treeViewParam.TreeViewHtml, treeViewParam.TotalIssues);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Disposed while the fetch was in flight.
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to fetch initial tree view from Language Server");
                }
            }).FireAndForget();
        }

        private void ExecuteCommandFromBridge(string command, string argsJson, string callbackId)
        {
            if (_disposed) return;

            // SnykVSPackage.ServiceProvider (or its LanguageClientManager) can be null during
            // startup/teardown. Dereferencing it directly here threw inside the FireAndForget lambda,
            // where the NRE was swallowed and the JS caller's callback promise hung forever. Resolve
            // defensively and, when unavailable, complete the callback with "null" (matching
            // DispatchAsync's null-result path) so the bridge promise resolves instead of hanging.
            var languageClientManager = SnykVSPackage.ServiceProvider?.LanguageClientManager;
            if (languageClientManager == null)
            {
                Logger.Warning("Cannot execute bridge command {Command}: LanguageClientManager unavailable", command);
                if (!string.IsNullOrEmpty(callbackId))
                    InvokeCommandCallback(callbackId, "null");
                return;
            }

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ExecuteCommandBridge.DispatchAsync(
                    languageClientManager,
                    command,
                    argsJson,
                    callbackId,
                    InvokeCommandCallback,
                    ctsToken);
            }).FireAndForget();
        }

        private void InvokeCommandCallback(string callbackId, string resultJson)
        {
            if (_disposed) return;

            if (!ExecuteCommandBridge.IsValidCallbackId(callbackId))
            {
                Logger.Warning("Rejected callbackId with unexpected format: {CallbackId}", callbackId);
                return;
            }

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ctsToken);
                    if (_disposed) return;
                    await host.ExecuteScriptAsync(
                        ExecuteCommandBridge.BuildCommandCallbackScript(callbackId, resultJson));
                }
                catch (OperationCanceledException)
                {
                    // Disposed before the callback could run.
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error invoking tree command callback {CallbackId}", callbackId);
                }
            }).FireAndForget();
        }

        // Disposes the underlying WebView2Host, which unsubscribes from WebMessageReceived and
        // sweeps any oversized-HTML temp files. Cancels in-flight LS replies / bridge callbacks so
        // they abandon their continuations rather than touch the disposed host. Called by
        // SnykToolWindowControl.Dispose.
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            cts.Cancel();
            cts.Dispose();
            host?.Dispose();
        }
    }
}
