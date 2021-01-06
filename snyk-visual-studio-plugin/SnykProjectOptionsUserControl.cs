using Snyk.VisualStudio.Extension.Services;
using System;
using System.Windows.Forms;

namespace Snyk.VisualStudio.Extension.UI
{
    public partial class SnykProjectOptionsUserControl : UserControl
    {
        private SnykSolutionService solutionService;

        public SnykProjectOptionsUserControl(SnykSolutionService solutionService)
        {
            InitializeComponent();

            this.solutionService = solutionService;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            bool isProjectOpened = solutionService.SolutionSettingsService.IsProjectOpened();

            additionalOptionsTextBox.Enabled = isProjectOpened;

            if (!isProjectOpened)
            {
                return;
            }

            try
            {
                string additionalOptions = solutionService.SolutionSettingsService.GetAdditionalOptions();

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
            if (solutionService.SolutionSettingsService.IsProjectOpened())
            {
                solutionService.SolutionSettingsService.SaveAdditionalOptions(additionalOptionsTextBox.Text.ToString());
            }
        }
    }
}
