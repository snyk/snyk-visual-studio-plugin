using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using Serilog;
using Snyk.VisualStudio.Extension.UI.Html;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    public partial class HtmlDescriptionPanel : UserControl, IDisposable
    {
        private static readonly ILogger Logger = LogManager.ForContext<HtmlDescriptionPanel>();

        private readonly IWebView2Host host;
        private IHtmlProvider htmlProvider;
        private bool _disposed;

        public HtmlDescriptionPanel()
        {
            this.InitializeComponent();

            // Themed surface before first render so the panel doesn't flash WebView2's dark
            // default while content loads. Must be set before CoreWebView2 initialization.
            HtmlViewer.DefaultBackgroundColor =
                VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);

            var bridge = new SnykScriptManager(SnykVSPackage.ServiceProvider);

            // Wire each window.X(...) call the LS HTML can make to the corresponding bridge method.
            // Messages arrive via chrome.webview.postMessage → WebMessageReceived → this dispatcher.
            var dispatcher = new WebView2MessageDispatcher()
                .Register("OpenFileInEditor", 5, args => bridge.OpenFileInEditor(
                    args[0].Value<string>(),
                    args[1].Value<string>(),
                    args[2].Value<string>(),
                    args[3].Value<string>(),
                    args[4].Value<string>()))
                .Register("OpenLink", 1, args => bridge.OpenLink(args[0].Value<string>()))
                .Register("EnableDelta", 1, args => bridge.EnableDelta(args[0].Value<bool>()))
                .Register("GenerateFixes", 1, args => bridge.GenerateFixes(args[0].Value<string>()))
                .Register("ApplyFixDiff", 1, args => bridge.ApplyFixDiff(args[0].Value<string>()))
                .Register("SubmitIgnoreRequest", 4, args => bridge.SubmitIgnoreRequest(
                    args[0].Value<string>(),
                    args[1].Value<string>(),
                    args[2].Value<string>(),
                    args[3].Value<string>()))
                .Register("FocusToolWindow", 0, _ => bridge.FocusToolWindow());

            // Shared with SummaryHtmlPanel — both controls live in the same tool window,
            // so they can safely share one browser process via a common user-data folder.
            var userDataFolder = WebView2Host.BuildUserDataFolder("toolwindow");

            host = new WebView2Host(HtmlViewer, dispatcher, userDataFolder);

            HtmlViewer.NavigationCompleted += OnNavigationCompleted;

            // Kick off WebView2 init now, but don't block the constructor — SetContent/Init below
            // await host.Ready internally, so queued navigations resolve once init completes.
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    // host.InitializeAsync drives WebView2/CoreWebView2 setup, which must run on the
                    // UI thread; RunAsync alone doesn't guarantee the continuation resumes there.
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    await host.InitializeAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "WebView2 initialization failed in description panel");
                }
            }).FireAndForget();
        }

        // The provider's init script (link-click interceptors, etc.) has to run after each
        // navigation completes, since NavigateAsync replaces the document. Skip when the
        // navigation itself failed — running the script against an error page would wire
        // the interceptors to the wrong document.
        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    if (!e.IsSuccess)
                    {
                        Logger.Warning(
                            "Description panel navigation failed (status {Status}); skipping init script",
                            e.WebErrorStatus);
                        return;
                    }
                    if (htmlProvider == null) return;
                    await host.ExecuteScriptAsync(htmlProvider.GetInitScript());
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error running description init script");
                }
            }).FireAndForget();
        }

        public void SetContent(string html, string product)
        {
            if (string.IsNullOrEmpty(html)) return;
            HtmlViewer.Visibility = Visibility.Visible;

            this.htmlProvider = HtmlProviderFactory.GetHtmlProvider(product);
            if (this.htmlProvider == null) return;

            var themedHtml = htmlProvider.ReplaceCssVariables(html);

            // Sync public API, async navigation — matches the pre-migration WebBrowser behaviour
            // so existing callers (tree-click handlers, error paths) don't need to be made async.
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await host.NavigateAsync(themedHtml);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to navigate description panel");
                }
            }).FireAndForget();
        }

        public void Init()
        {
            HtmlViewer.Visibility = Visibility.Collapsed;

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await host.NavigateAsync("<html><body style='margin:0;padding:0;'>Loading...</body></html>");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to initialize description panel content");
                }
            }).FireAndForget();
        }

        // Disposes the underlying WebView2Host, which unsubscribes from WebMessageReceived
        // and sweeps any oversized-HTML temp files left in the scratch directory. Called
        // by SnykToolWindowControl.Dispose when ToolWindowPane tears down the tool window.
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            HtmlViewer.NavigationCompleted -= OnNavigationCompleted;
            host?.Dispose();
        }
    }
}
