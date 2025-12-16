using System;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    public partial class SnykExperimentalUserControl : UserControl
    {
        private readonly ISnykServiceProvider serviceProvider;
        public ISnykOptions OptionsMemento { get; set; }

        public SnykExperimentalUserControl(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            OptionsMemento = serviceProvider.SnykOptionsManager.Load();
            this.Load += OnLoad;
            InitializeComponent();
            this.UpdateViewFromOptions();
        }

        private void UpdateViewFromOptions()
        {
            this.cbIgnoredIssues.Checked = OptionsMemento.IgnoredIssuesEnabled;
            this.cbOpenIssues.Checked = OptionsMemento.OpenIssuesEnabled;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            CheckForIgnores();
        }

        private void CheckForIgnores()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                if (serviceProvider.Options.ApiToken.IsValid() && !serviceProvider.Options.ConsistentIgnoresEnabled && LanguageClientHelper.IsLanguageServerReady())
                    await serviceProvider.FeatureFlagService.RefreshAsync(SnykVSPackage.Instance.DisposalToken);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }).FireAndForget();
        }

        private void cbOpenIssues_CheckedChanged(object sender, System.EventArgs e)
        {
            OptionsMemento.OpenIssuesEnabled = cbOpenIssues.Checked;
        }

        private void cbIgnoredIssues_CheckedChanged(object sender, System.EventArgs e)
        {
            OptionsMemento.IgnoredIssuesEnabled = cbIgnoredIssues.Checked;
        }

        private void btnOpenSettingsV2_Click(object sender, EventArgs e)
        {
            try
            {
                // Get required dependencies
                var options = serviceProvider.Options;
                var languageServerRpc = serviceProvider.LanguageClientManager?.Rpc;
                var optionsManager = serviceProvider.SnykOptionsManager;

                // Create and show modal window
                var settingsWindow = new HtmlSettingsWindow(
                    options,
                    languageServerRpc,
                    optionsManager,
                    serviceProvider);

                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to open settings window: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
