// ABOUTME: This file implements the WinForms user control for user experience settings
// ABOUTME: It provides UI for configuring telemetry, auto-scan, error reporting, and issue visibility preferences
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    public partial class SnykUserExperienceUserControl : BaseSnykUserControl
    {
        public SnykUserExperienceUserControl(ISnykServiceProvider serviceProvider) : base(serviceProvider)
        {
            InitializeComponent();
            UpdateViewFromOptions();
        }

        protected override void UpdateViewFromOptions()
        {
            this.autoScanCheckBox.Checked = OptionsMemento.AutoScan;
        }

        private void autoScanCheckBox_CheckedChanged(object sender, System.EventArgs e)
        {
            OptionsMemento.AutoScan = autoScanCheckBox.Checked;
        }
    }
}
