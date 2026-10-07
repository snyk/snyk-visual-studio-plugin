using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Serilog;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Interaction logic for MessagePanel.xaml.
    /// </summary>
    public partial class MessagePanel : UserControl
    {
        private static readonly ILogger Logger = LogManager.ForContext<MessagePanel>();
        private readonly IList<StackPanel> panels;
        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePanel"/> class.
        /// </summary>
        public MessagePanel()
        {
            this.InitializeComponent();

            this.panels = new List<StackPanel>
            {
                this.selectIssueMessagePanel,
                this.noIssuesMessagePanel,
                this.runScanMessagePanel,
                this.trustFolderMessagePanel,
                this.messagePanel,
                this.overviewPanel,
                this.scanningProjectMessagePanel,
                this.snykInitializing
            };
            snykDogLogo.Source = SnykIconProvider.GetImageSourceFromPath(SnykIconProvider.SnykDogLogoIconPath);
            var languageClientManager = LanguageClientHelper.LanguageClientManager();
            if (languageClientManager != null)
            {
                languageClientManager.OnLanguageServerReadyAsync += LanguageClientManagerOnOnLanguageServerReady;
            }
        }

        private async Task LanguageClientManagerOnOnLanguageServerReady(object sender, SnykLanguageServerEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            testCodeNowButton.IsEnabled = true;
        }

        /// <summary>
        /// Gets or sets <see cref="ISnykServiceProvider"/> instance.
        /// </summary>
        public ISnykServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Gets or sets <see cref="ToolWindowContext"/> instance.
        /// </summary>
        public ToolWindowContext Context { get; set; }

        /// <summary>
        /// Sets text on the <see cref="messagePanel"/> and shows it.
        /// </summary>
        public string Text
        {
            set
            {
                this.message.Text = value;

                this.ShowPanel(this.messagePanel);
            }
        }

        /// <summary>
        /// Show run scan message.
        /// </summary>
        public void ShowRunScanMessage() => this.ShowPanel(this.runScanMessagePanel);

        /// <summary>
        /// Show the run scan message, or the trust prompt when the open folder is not trusted yet.
        /// </summary>
        public async Task ShowRunScanOrTrustFolderMessageAsync()
        {
            var untrustedFolder = await this.GetUntrustedSolutionFolderAsync();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (untrustedFolder == null)
            {
                this.ShowRunScanMessage();
                return;
            }

            this.untrustedFolderPath.Text = untrustedFolder;
            this.trustFolderButton.IsEnabled = true;
            this.ShowPanel(this.trustFolderMessagePanel);
        }

        private async Task<string> GetUntrustedSolutionFolderAsync()
        {
            if (this.ServiceProvider?.SolutionService == null || this.ServiceProvider.WorkspaceTrustService == null)
            {
                return null;
            }

            var folder = await this.ServiceProvider.SolutionService.GetSolutionFolderAsync();
            if (string.IsNullOrEmpty(folder) || this.ServiceProvider.WorkspaceTrustService.IsFolderTrusted(folder))
            {
                return null;
            }

            return folder;
        }

        /// <summary>
        /// Show select issue message.
        /// </summary>
        public void ShowSelectIssueMessage() => this.ShowPanel(this.selectIssueMessagePanel);

        /// <summary>
        /// Show scanning message.
        /// </summary>
        public void ShowScanningMessage() => this.ShowPanel(this.scanningProjectMessagePanel);

        /// <summary>
        /// Show overview screen message.
        /// </summary>
        public void ShowOverviewScreenMessage(bool signInCancelled = false)
        {
            if (!LanguageClientHelper.IsLanguageServerReady())
            {
                testCodeNowButton.IsEnabled = false;
            }

            // The overview is only shown while the token is not valid, so a user who had a session is seeing an expired one.
            var options = this.ServiceProvider?.Options;
            var sessionExpired = options?.HadSession == true || !string.IsNullOrEmpty(options?.ApiToken?.ToString());

            this.welcomePanel.Visibility = sessionExpired ? Visibility.Collapsed : Visibility.Visible;
            this.sessionExpiredPanel.Visibility = sessionExpired ? Visibility.Visible : Visibility.Collapsed;
            this.signInCancelledPanel.Visibility = signInCancelled ? Visibility.Visible : Visibility.Collapsed;

            this.ShowPanel(this.overviewPanel);
        }

        public void ShowInitializingScreenMessage()
        {
            this.ShowPanel(this.snykInitializing);
        }

        private void RunButton_Click(object sender, RoutedEventArgs e) => ThreadHelper.JoinableTaskFactory.RunAsync(SnykTasksService.Instance.ScanAsync).FireAndForget();

        private void TrustFolder_Click(object sender, RoutedEventArgs e)
        {
            var folder = this.untrustedFolderPath.Text;
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            this.trustFolderButton.IsEnabled = false;
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await this.ServiceProvider.LanguageClientManager.InvokeExecuteCommandAsync(
                        LsConstants.SnykTrustWorkspaceFolders, new object[] { folder }, SnykVSPackage.Instance.DisposalToken);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to trust folder {Folder}", folder);
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    this.trustFolderButton.IsEnabled = true;
                }
            }).FireAndForget();
        }

        private void ShowPanel(StackPanel panel)
        {
            foreach (var stackPanel in this.panels)
            {
                stackPanel.Visibility = Visibility.Collapsed;
            }

            panel.Visibility = Visibility.Visible;
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            this.testCodeNowButton.IsEnabled = false;
            ThreadHelper.JoinableTaskFactory.RunAsync(SignInAsync).FireAndForget();
        }

        // Folder trust is not touched here: the language server asks for it in the tree view before the first scan.
        private async Task SignInAsync()
        {
            try
            {
                Logger.Information("Attempting to Auth");

                // TODO: REMOVE BEFORE MERGING - fakes a successful OAuth sign-in instead of opening the
                // browser, so the signed-in tool window can be exercised where the Okta login fails.
                var options = this.ServiceProvider.Options;
                options.AuthenticationMethod = AuthenticationType.OAuth;
                options.ApiToken = new AuthenticationToken(
                    AuthenticationType.OAuth,
                    "{\"access_token\":\"fake-sign-in\",\"token_type\":\"Bearer\",\"refresh_token\":\"fake\",\"expiry\":\"2099-01-01T00:00:00Z\"}");
                options.HadSession = true;
                this.ServiceProvider.SnykOptionsManager.Save(options, triggerSettingsChangedEvent: false, updateOverrideTracker: false);
                // TODO: REMOVE BEFORE MERGING - end; restore the line below.
                // this.ServiceProvider.AuthenticationFlowService.Authenticate();
            }
            catch (FileNotFoundException)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.Text = "Snyk CLI not found. You can specify a path to a Snyk CLI executable from the settings.";
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            this.testCodeNowButton.IsEnabled = true;

            if (this.ServiceProvider.Options.ApiToken.IsValid())
            {
                this.Context.TransitionTo(RunScanState.Instance);
            }
            else if (this.ServiceProvider.Options.AuthenticationMethod != AuthenticationType.Pat)
            {
                this.ShowOverviewScreenMessage(signInCancelled: true);
            }
        }


        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs args)
        {
            Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri));

            args.Handled = true;
        }

        private void InitializeNowButton_OnClick(object sender, RoutedEventArgs e)
        {
            var languageClientManager = LanguageClientHelper.LanguageClientManager();
            if (languageClientManager == null) return;
            languageClientManager.FireOnLanguageClientNotInitializedAsync();
        }
    }
}
