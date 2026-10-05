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
        private bool sessionExpired;

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
        public async Task ShowOverviewScreenMessageAsync(bool signInCancelled = false)
        {
            // The overview is only shown while the token is not valid, so a non-empty token means it expired.
            // A cancelled sign-in has already logged out, so keep the heading from before the click.
            if (!signInCancelled)
            {
                this.sessionExpired = !string.IsNullOrEmpty(this.ServiceProvider?.Options?.ApiToken?.ToString());
            }

            var needsTrust = await this.CurrentFolderNeedsTrustAsync();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!LanguageClientHelper.IsLanguageServerReady())
            {
                testCodeNowButton.IsEnabled = false;
            }

            this.welcomePanel.Visibility = this.sessionExpired ? Visibility.Collapsed : Visibility.Visible;
            this.sessionExpiredPanel.Visibility = this.sessionExpired ? Visibility.Visible : Visibility.Collapsed;
            this.trustPanel.Visibility = needsTrust ? Visibility.Visible : Visibility.Collapsed;
            this.signInCancelledPanel.Visibility = signInCancelled ? Visibility.Visible : Visibility.Collapsed;
            this.testCodeNowButton.Content = needsTrust ? "Trust project and sign in" : "Sign in";

            this.ShowPanel(this.overviewPanel);
        }

        private async Task<bool> CurrentFolderNeedsTrustAsync()
        {
            if (this.ServiceProvider == null)
            {
                return false;
            }

            var folder = await this.ServiceProvider.SolutionService.GetSolutionFolderAsync();
            return !string.IsNullOrEmpty(folder) && !this.ServiceProvider.WorkspaceTrustService.IsFolderTrusted(folder);
        }

        public void ShowInitializingScreenMessage()
        {
            this.ShowPanel(this.snykInitializing);
        }

        private void RunButton_Click(object sender, RoutedEventArgs e) => ThreadHelper.JoinableTaskFactory.RunAsync(SnykTasksService.Instance.ScanAsync).FireAndForget();

        private void ShowPanel(StackPanel panel)
        {
            foreach (var stackPanel in this.panels)
            {
                stackPanel.Visibility = Visibility.Collapsed;
            }

            panel.Visibility = Visibility.Visible;
        }

        private void TestCodeNow_Click(object sender, RoutedEventArgs e)
        {
            this.testCodeNowButton.IsEnabled = false;
            ThreadHelper.JoinableTaskFactory.RunAsync(RunTestCodeNowAsync).FireAndForget();
        }
        private async Task RunTestCodeNowAsync()
        {
            Logger.Information("Enter RunTestCodeNowAsync");
            var solutionFolderPath = await this.ServiceProvider.SolutionService.GetSolutionFolderAsync();
            if (!string.IsNullOrEmpty(solutionFolderPath) && !this.ServiceProvider.WorkspaceTrustService.IsFolderTrusted(solutionFolderPath))
            {
                Logger.Information("Solution Folder Is {SolutionFolder}", solutionFolderPath);

                try
                {
                    ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        Logger.Information("Adding Folder {SolutionFolder} to trusted folders", solutionFolderPath);

                        this.ServiceProvider.WorkspaceTrustService.AddFolderToTrusted(solutionFolderPath);
                        Logger.Information("Workspace folder was trusted: {SolutionFolderPath}", solutionFolderPath);
                        await this.ServiceProvider.LanguageClientManager.DidChangeConfigurationAsync(SnykVSPackage
                            .Instance.DisposalToken);
                    }).FireAndForget();
                    
                }
                catch (ArgumentException ex)
                {
                    Logger.Error(ex, "Failed to add folder to trusted list.");
                    throw;
                }
            }

            try
            {
                Logger.Information("Attempting to Auth");
                this.ServiceProvider.AuthenticationFlowService.Authenticate();
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
                await this.ShowOverviewScreenMessageAsync(signInCancelled: true);
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
