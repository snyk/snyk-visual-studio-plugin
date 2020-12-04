using Snyk.VisualStudio.Extension.Services;
using Snyk.VisualStudio.Extension.Settings;
using System;
using System.Windows.Forms;

namespace Snyk.VisualStudio.Extension.UI
{
    public partial class SnykProjectOptionsUserControl : UserControl
    {       
        private SnykProjectSettingsService projectSettingsService;

        public SnykProjectOptionsUserControl(SnykSolutionService solutionService)
        {
            InitializeComponent();

            this.projectSettingsService = new SnykProjectSettingsService(solutionService);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            bool isProjectOpened = projectSettingsService.IsProjectOpened();

            additionalOptionsTextBox.Enabled = isProjectOpened;

            if (!isProjectOpened)
            {
                return;
            }

            try
            {
                string additionalOptions = projectSettingsService.GetAdditionalOptions();

                if (!String.IsNullOrEmpty(additionalOptions))
                {
                    additionalOptionsTextBox.Text = additionalOptions;
                } else
                {
                    additionalOptionsTextBox.Text = "";
                }
            }
            catch (Exception exception)
            {
                additionalOptionsTextBox.Text = "";
            }
        }        

        private void additionalOptionsTextBox_TextChanged(object sender, EventArgs e)
        {
            if (projectSettingsService.IsProjectOpened())
            {
                projectSettingsService.SaveAdditionalOptions(additionalOptionsTextBox.Text.ToString());
            }
        }
    }
}
