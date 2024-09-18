using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Serilog;
using Snyk.Common;
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
        public void ShowOverviewScreenMessage()
        {
            if (!LanguageClientHelper.IsLanguageServerReady())
            {
                testCodeNowButton.IsEnabled = false;
            }

            this.ShowPanel(this.overviewPanel);
        }

        public void ShowInitializingScreenMessage()
        {
            this.ShowPanel(this.snykInitializing);
        }

        private void RunButton_Click(object sender, RoutedEventArgs e) => ThreadHelper.JoinableTaskFactory.RunAsync(SnykTasksService.Instance.ScanAsync);

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
            ThreadHelper.JoinableTaskFactory.Run(RunTestCodeNowAsync);
        }

        private async Task RunTestCodeNowAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            this.testCodeNowButton.IsEnabled = false; // Disable the button while authenticating
            this.authenticateSnykProgressBar.Visibility = Visibility.Visible;

            await TaskScheduler.Default;
            bool authenticationSucceeded;
            try
            {
                authenticationSucceeded = this.ServiceProvider.Options.Authenticate();
            }
            catch (FileNotFoundException)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.authenticateSnykProgressBar.Visibility = Visibility.Collapsed;
                this.Text = "Snyk CLI not found. You can specify a path to a Snyk CLI executable from the settings.";
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            this.authenticateSnykProgressBar.Visibility = Visibility.Collapsed;
            this.testCodeNowButton.IsEnabled = true;

            // Add folder to trusted
            var solutionFolderPath = await this.ServiceProvider.SolutionService.GetSolutionFolderAsync();
            if (!string.IsNullOrEmpty(solutionFolderPath))
            {
                try
                {
                    this.ServiceProvider.WorkspaceTrustService.AddFolderToTrusted(solutionFolderPath);
                    Logger.Information("Workspace folder was trusted: {SolutionFolderPath}", solutionFolderPath);
                }
                catch (ArgumentException ex)
                {
                    Logger.Error(ex, "Failed to add folder to trusted list.");
                    throw;
                }
            }

            // Issue scan
            if (authenticationSucceeded)
            {
                var uiShell = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;
                if (uiShell != null)
                {
                    uiShell.PostExecCommand(
                        SnykGuids.SnykVSPackageCommandSet,
                        SnykGuids.RunScanCommandId,
                        0,
                        null);
                }
            }

            var nextPanel = authenticationSucceeded ? (ToolWindowState)RunScanState.Instance : OverviewState.Instance;
            this.Context.TransitionTo(nextPanel);
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs args)
        {
            Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri));

            args.Handled = true;
        }
    }
}
