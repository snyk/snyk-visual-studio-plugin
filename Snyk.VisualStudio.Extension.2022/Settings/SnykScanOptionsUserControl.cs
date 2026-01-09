// ABOUTME: This file implements the WinForms user control for scan-related settings
// ABOUTME: It provides UI for enabling/disabling security products (Code, OSS, IaC) and configuring severity filters
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.UI.Notifications;

namespace Snyk.VisualStudio.Extension.Settings
{
    public partial class SnykScanOptionsUserControl : BaseSnykUserControl
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykScanOptionsUserControl>();

        public SnykScanOptionsUserControl(ISnykServiceProvider serviceProvider) : base(serviceProvider)
        {
            InitializeComponent();
            Logger.Information("Initializing SnykScanOptionsUserControl");

            UpdateViewFromOptions();
            this.Load += this.SnykScanOptionsUserControl_Load;
            this.serviceProvider.ToolWindow.Show();

            Logger.Information("SnykScanOptionsUserControl initialized");
        }

        private void SnykScanOptionsUserControl_Load(object sender, EventArgs e)
        {
            // SAST enablement check removed - checkbox is always toggleable
        }

        protected override void UpdateViewFromOptions()
        {
            this.ossEnabledCheckBox.Checked = OptionsMemento.OssEnabled;
            this.iacEnabledCheckbox.Checked = OptionsMemento.IacEnabled;
            this.codeSecurityEnabledCheckBox.Checked = OptionsMemento.SnykCodeSecurityEnabled;

            if (cbDelta.DataSource == null)
            {
                this.cbDelta.DataSource = DeltaOptionList();
            }
            this.cbDelta.SelectedItem = OptionsMemento.EnableDeltaFindings ? "Net new issues" : "All issues";
        }

        private IEnumerable<string> DeltaOptionList()
        {
            var defaultList = new List<string> { "All issues", "Net new issues" };
            return defaultList;
        }

        private void OssEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            OptionsMemento.OssEnabled = this.ossEnabledCheckBox.Checked;
        }

        private void iacEnabledCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            OptionsMemento.IacEnabled = iacEnabledCheckbox.Checked;
        }

        private void CodeSecurityEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            OptionsMemento.SnykCodeSecurityEnabled = this.codeSecurityEnabledCheckBox.Checked;
        }
        
        private void cbDelta_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (this.cbDelta.SelectedItem == null)
                return;
            var enableDelta = this.cbDelta.SelectedItem.ToString() == "Net new issues";
            OptionsMemento.EnableDeltaFindings = enableDelta;
        }

        private void SnykCodeSettingsLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            => Process.Start(OptionsMemento.SnykCodeSettingsUrl);

    }
}
