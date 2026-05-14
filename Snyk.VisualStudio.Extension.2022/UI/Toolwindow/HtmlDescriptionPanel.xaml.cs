using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using Serilog;
using Snyk.VisualStudio.Extension.UI.Html;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    public partial class HtmlDescriptionPanel : UserControl
    {
        private static readonly ILogger Logger = LogManager.ForContext<HtmlDescriptionPanel>();

        private readonly WebView2Host host;
        private IHtmlProvider htmlProvider;

        public HtmlDescriptionPanel()
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

            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Snyk", "WebView2", "description");

            host = new WebView2Host(HtmlViewer, dispatcher, userDataFolder);

            HtmlViewer.NavigationCompleted += OnNavigationCompleted;

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await host.InitializeAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "WebView2 initialization failed in description panel");
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
                Logger.Error(ex, "Error running description init script");
            }
        }

        public void SetContent(string html, string product)
        {
            if (string.IsNullOrEmpty(html)) return;
            HtmlViewer.Visibility = Visibility.Visible;

            this.htmlProvider = HtmlProviderFactory.GetHtmlProvider(product);
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
    }
}
