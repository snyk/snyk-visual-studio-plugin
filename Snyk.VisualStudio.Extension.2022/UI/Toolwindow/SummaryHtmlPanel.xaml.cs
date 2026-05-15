using System;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using Serilog;
using Snyk.VisualStudio.Extension.UI.Html;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    public partial class SummaryHtmlPanel : UserControl, IDisposable
    {
        private static readonly ILogger Logger = LogManager.ForContext<SummaryHtmlPanel>();

        private readonly WebView2Host host;
        private IHtmlProvider htmlProvider;
        private bool _disposed;

        public SummaryHtmlPanel()
        {
            this.InitializeComponent();

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

            // Shared with HtmlDescriptionPanel — see comment there.
            var userDataFolder = WebView2Host.BuildUserDataFolder("toolwindow");

            host = new WebView2Host(SummaryHtmlViewer, dispatcher, userDataFolder);

            SummaryHtmlViewer.NavigationCompleted += OnNavigationCompleted;

            // Kick off WebView2 init now, but don't block the constructor — SetContent/Init below
            // await host.Ready internally, so queued navigations resolve once init completes.
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await host.InitializeAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "WebView2 initialization failed in summary panel");
                }
            }).FireAndForget();
        }

        // The provider's init script (link-click interceptors, etc.) has to run after each
        // navigation completes, since NavigateAsync replaces the document.
        private async void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                if (htmlProvider == null) return;
                await host.ExecuteScriptAsync(htmlProvider.GetInitScript());
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error running summary init script");
            }
        }

        public void SetContent(string html, string product)
        {
            if (string.IsNullOrEmpty(html)) return;

            this.htmlProvider = HtmlProviderFactory.GetHtmlProvider("summary");
            if (this.htmlProvider == null) return;

            var themedHtml = htmlProvider.ReplaceCssVariables(html);

            // Sync public API, async navigation — matches the pre-migration WebBrowser behaviour
            // so existing callers (scan-finished notification, error paths) don't need to be async.
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await host.NavigateAsync(themedHtml);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to navigate summary panel");
                }
            }).FireAndForget();
        }

        // Loads the embedded "Snyk Security is loading…" placeholder before the first scan
        // completes. Provider is loaded lazily because StaticHtmlProvider reads from disk.
        public void Init()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    var provider = (StaticHtmlProvider)HtmlProviderFactory.GetHtmlProvider("static");
                    var html = provider.ReplaceCssVariables(await provider.GetInitHtmlAsync());
                    await host.NavigateAsync(html);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to initialize summary panel content");
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
            host?.Dispose();
        }
    }
}
