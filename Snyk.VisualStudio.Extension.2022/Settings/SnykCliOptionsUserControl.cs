// ABOUTME: This file implements the WinForms user control for Snyk CLI-related settings
// ABOUTME: It provides UI for configuring CLI path, release channel, download URL, and automatic updates
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    public partial class SnykCliOptionsUserControl : BaseSnykUserControl
    {
        public SnykCliOptionsUserControl(ISnykServiceProvider serviceProvider) : base(serviceProvider)
        {
            InitializeComponent();
            UpdateViewFromOptions();
        }

        protected override void UpdateViewFromOptions()
        {
            this.manageBinariesAutomaticallyCheckbox.Checked = OptionsMemento.BinariesAutoUpdate;
            this.cliDownloadUrlTextBox.Text = OptionsMemento.CliBaseDownloadURL;

            var cliPath = string.IsNullOrEmpty(OptionsMemento.CliCustomPath)
                ? SnykCli.GetSnykCliDefaultPath()
                : OptionsMemento.CliCustomPath;

            this.CliPathTextBox.Text = cliPath;
            if (releaseChannel.DataSource == null)
            {
                this.releaseChannel.DataSource = ReleaseChannelList();
            }

            this.releaseChannel.SelectedItem = OptionsMemento.CliReleaseChannel;
        }

        private IEnumerable<string> ReleaseChannelList()
        {
            var defaultList = new List<string>() { "stable", "rc", "preview" };
            if (!defaultList.Contains(OptionsMemento.CliReleaseChannel))
            {
                defaultList.Add(OptionsMemento.CliReleaseChannel);
            }
            return defaultList;
        }

        private void CliPathBrowseButton_Click(object sender, System.EventArgs e)
        {
            if(this.customCliPathFileDialog.ShowDialog() == DialogResult.OK)
            {
                var selectedCliPath = this.customCliPathFileDialog.FileName;
                this.SetCliCustomPathValue(selectedCliPath);
            }
        }

        private void SetCliCustomPathValue(string selectedCliPath)
        {
            OptionsMemento.CliCustomPath = selectedCliPath;
            this.CliPathTextBox.Text = string.IsNullOrEmpty(OptionsMemento.CliCustomPath)
                ? SnykCli.GetSnykCliDefaultPath()
                : selectedCliPath;
        }

        private void ClearCliCustomPathButton_Click(object sender, System.EventArgs e)
        {
            this.SetCliCustomPathValue(string.Empty);
        }

        private void ReleaseChannelLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.ReleaseChannelLink.LinkVisited = true;
            Process.Start("https://docs.snyk.io/snyk-cli/releases-and-channels-for-the-snyk-cli");
        }
        private void manageBinariesAutomaticallyCheckbox_CheckedChanged(object sender, System.EventArgs e)
        {
            OptionsMemento.BinariesAutoUpdate = manageBinariesAutomaticallyCheckbox.Checked;
        }

        private void releaseChannel_SelectionChangeCommitted(object sender, System.EventArgs e)
        {
            var selectedItem = this.releaseChannel.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(selectedItem))
                return;
            OptionsMemento.CliReleaseChannel = selectedItem;
        }
    }
}
