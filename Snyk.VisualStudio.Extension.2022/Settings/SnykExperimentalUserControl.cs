// ABOUTME: This file implements the WinForms user control for experimental feature settings
// ABOUTME: It provides UI for configuring preview features like delta findings and consistent ignores
using System;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    public partial class SnykExperimentalUserControl : BaseSnykUserControl
    {
        public SnykExperimentalUserControl(ISnykServiceProvider serviceProvider) : base(serviceProvider)
        {
            this.Load += OnLoad;
            InitializeComponent();
            UpdateViewFromOptions();
        }

        protected override void UpdateViewFromOptions()
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
                // Create and show modal window
                using (DpiContextScope.EnterUnawareGdiScaled())
                {
                    var settingsWindow = new HtmlSettingsWindow(serviceProvider);
                    settingsWindow.ShowDialog();
                }
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
