using System;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using Serilog;
using Snyk.VisualStudio.Extension.UI.Html;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    public partial class SummaryHtmlPanel : UserControl
    {
        private static readonly ILogger Logger = LogManager.ForContext<SummaryHtmlPanel>();

        private readonly WebView2Host host;
        private IHtmlProvider htmlProvider;

        public SummaryHtmlPanel()
        {
            this.InitializeComponent();

            var bridge = new SnykScriptManager(SnykVSPackage.ServiceProvider);

            var dispatcher = new WebView2MessageDispatcher()
                .Register("OpenFileInEditor", args => bridge.OpenFileInEditor(
                    args[0].Value<string>(),
                    args[1].Value<string>(),
                    args[2].Value<string>(),
                    args[3].Value<string>(),
                    args[4].Value<string>()))
                .Register("OpenLink", args => bridge.OpenLink(args[0].Value<string>()))
                .Register("EnableDelta", args => bridge.EnableDelta(args[0].Value<bool>()))
                .Register("GenerateFixes", args => bridge.GenerateFixes(args[0].Value<string>()))
                .Register("ApplyFixDiff", args => bridge.ApplyFixDiff(args[0].Value<string>()))
                .Register("SubmitIgnoreRequest", args => bridge.SubmitIgnoreRequest(
                    args[0].Value<string>(),
                    args[1].Value<string>(),
                    args[2].Value<string>(),
                    args[3].Value<string>()))
                .Register("FocusToolWindow", _ => bridge.FocusToolWindow());

            var scratchDirectory = WebView2EnvironmentProvider.GetScratchDirectory("summary");

            host = new WebView2Host(SummaryHtmlViewer, dispatcher, scratchDirectory);

            SummaryHtmlViewer.NavigationCompleted += OnNavigationCompleted;

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
    }
}
