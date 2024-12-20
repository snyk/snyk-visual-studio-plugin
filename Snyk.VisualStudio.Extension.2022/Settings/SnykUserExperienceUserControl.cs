using System.Windows.Forms;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    public partial class SnykUserExperienceUserControl : UserControl
    {
        private readonly ISnykServiceProvider serviceProvider;
        public ISnykOptions OptionsMemento { get; set; }

        public SnykUserExperienceUserControl(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            OptionsMemento = serviceProvider.SnykOptionsManager.Load();
            InitializeComponent();
            this.UpdateViewFromOptions();
        }

        private void UpdateViewFromOptions()
        {
            this.autoScanCheckBox.Checked = OptionsMemento.AutoScan;
        }

        private void autoScanCheckBox_CheckedChanged(object sender, System.EventArgs e)
        {
            OptionsMemento.AutoScan = autoScanCheckBox.Checked;
        }
    }
}
