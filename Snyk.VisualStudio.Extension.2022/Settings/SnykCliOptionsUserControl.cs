using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    public partial class SnykCliOptionsUserControl : UserControl
    {
        private readonly ISnykServiceProvider serviceProvider;
        private readonly ISnykOptions snykOptions;
        private static readonly ILogger Logger = LogManager.ForContext<SnykCliOptionsUserControl>();

        public SnykCliOptionsUserControl(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            snykOptions = this.serviceProvider.Options;
            InitializeComponent();
            this.Initialize();
        }
        private void Initialize()
        {
            this.UpdateViewFromOptions();
        }
 
        private void UpdateViewFromOptions()
        {
            this.manageBinariesAutomaticallyCheckbox.Checked = snykOptions.BinariesAutoUpdate;
            this.cliDownloadUrlTextBox.Text = snykOptions.CliDownloadUrl;

            var cliPath = string.IsNullOrEmpty(snykOptions.CliCustomPath)
                ? SnykCli.GetSnykCliDefaultPath()
                : snykOptions.CliCustomPath;

            this.CliPathTextBox.Text = cliPath;
            if (releaseChannel.DataSource == null)
            {
                this.releaseChannel.DataSource = ReleaseChannelList();
            }

            this.releaseChannel.SelectedItem = snykOptions.CliReleaseChannel;
        }

        private IEnumerable<string> ReleaseChannelList()
        {
            var defaultList = new List<string>() { "stable", "rc", "preview" };
            if (!defaultList.Contains(snykOptions.CliReleaseChannel))
            {
                defaultList.Add(snykOptions.CliReleaseChannel);
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
            snykOptions.CliCustomPath = selectedCliPath;
            this.CliPathTextBox.Text = string.IsNullOrEmpty(snykOptions.CliCustomPath)
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

        public string GetReleaseChannel()
        {
            return releaseChannel.Text;
        }

        public string GetCliDownloadUrl()
        {
            return cliDownloadUrlTextBox.Text;
        }

        public bool GetManageBinariesAutomatically()
        {
            return manageBinariesAutomaticallyCheckbox.Checked;
        }

        public Panel GetPanel()
        {
            return this.mainPanel;
        }
    }
}
