using Snyk.VisualStudio.Extension.CLI;
using System;
using System.Windows.Forms;

namespace Snyk.VisualStudio.Extension.Settings
{
    public partial class SnykProjectOptionsUserControl : UserControl
    {
        private SnykSolutionService solutionService;

        public SnykProjectOptionsUserControl(SnykSolutionService solutionService)
        {
            InitializeComponent();

            this.solutionService = solutionService;
        }    

        private void checkOptionConflicts()
        {
            if (allProjectsCheckBox.Checked && additionalOptionsTextBox.Text.Contains("--file="))
            {
                errorProvider.SetError(additionalOptionsTextBox, 
                    "The following option combination is not currently supported: file + all-projects");
            } 
            else
            {
                errorProvider.SetError(additionalOptionsTextBox, "");
            }
        }

        private void additionalOptionsTextBox_TextChanged(object sender, EventArgs e)
        {
            if (solutionService.IsSolutionOpen)
            {                
                solutionService.SolutionSettingsService.SaveAdditionalOptions(additionalOptionsTextBox.Text.ToString());

                checkOptionConflicts();
            }
        }

        private void allProjectsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (solutionService.IsSolutionOpen)
            {
                solutionService.SolutionSettingsService.SaveIsAllProjectsScanEnabled(allProjectsCheckBox.Checked);

                checkOptionConflicts();
            }
        }

        private void SnykProjectOptionsUserControl_Load(object sender, EventArgs eventArgs)
        {
            base.OnVisibleChanged(eventArgs);

            bool isProjectOpened = solutionService.IsSolutionOpen;

            additionalOptionsTextBox.Enabled = isProjectOpened;
            allProjectsCheckBox.Enabled = isProjectOpened;

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
                }
                else
                {
                    additionalOptionsTextBox.Text = "";
                }
            }
            catch (Exception exception)
            {
                solutionService.Logger.LogError(exception.Message);

                additionalOptionsTextBox.Text = "";
            }

            try
            {
                allProjectsCheckBox.Checked = solutionService.SolutionSettingsService.GetIsAllProjectsEnabled();
            }
            catch (Exception exception)
            {
                solutionService.Logger.LogError(exception.Message);

                allProjectsCheckBox.Checked = false;

                solutionService.SolutionSettingsService.SaveIsAllProjectsScanEnabled(false);
            }

            checkOptionConflicts();
        }
    }
}
