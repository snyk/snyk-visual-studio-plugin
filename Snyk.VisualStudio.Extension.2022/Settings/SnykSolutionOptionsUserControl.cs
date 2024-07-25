using System;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.Common;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Solution settings control.
    /// </summary>
    public partial class SnykSolutionOptionsUserControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykSolutionOptionsUserControl>();

        private ISnykServiceProvider serviceProvider;

        private SnykUserStorageSettingsService userStorageSettingsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykSolutionOptionsUserControl"/> class.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider.</param>
        public SnykSolutionOptionsUserControl(ISnykServiceProvider serviceProvider)
        {
            this.InitializeComponent();

            this.serviceProvider = serviceProvider;
            this.userStorageSettingsService = serviceProvider.UserStorageSettingsService;
        }

        private void CheckOptionConflicts()
        {
            if (this.allProjectsCheckBox.Checked && this.additionalOptionsTextBox.Text.Contains("--file="))
            {
                this.errorProvider.SetError(
                    this.additionalOptionsTextBox,
                    "The following option combination is not currently supported: file + all-projects");
            }
            else
            {
                this.errorProvider.SetError(this.additionalOptionsTextBox, string.Empty);
            }
        }

        private void AdditionalOptionsTextBox_TextChanged(object sender, EventArgs e)
        {
            if (this.serviceProvider.SolutionService.IsSolutionOpen())
            {
                string additionalOptions = this.additionalOptionsTextBox.Text.ToString();

                this.userStorageSettingsService.SaveAdditionalOptionsAsync(additionalOptions).FireAndForget();

                this.CheckOptionConflicts();
            }
        }

        private void AllProjectsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.serviceProvider.SolutionService.IsSolutionOpen())
            {
                this.userStorageSettingsService.SaveIsAllProjectsScanEnabledAsync(this.allProjectsCheckBox.Checked).FireAndForget(); ;

                this.CheckOptionConflicts();
            }
        }

        private void SnykProjectOptionsUserControl_Load(object sender, EventArgs eventArgs) => ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            this.OnVisibleChanged(eventArgs);

            bool isProjectOpened = this.serviceProvider.SolutionService.IsSolutionOpen();

            this.additionalOptionsTextBox.Enabled = isProjectOpened;
            this.allProjectsCheckBox.Enabled = isProjectOpened;

            if (!isProjectOpened)
            {
                return;
            }

            try
            {
                string additionalOptions = await this.userStorageSettingsService.GetAdditionalOptionsAsync();

                if (!string.IsNullOrEmpty(additionalOptions))
                {
                    this.additionalOptionsTextBox.Text = additionalOptions;
                }
                else
                {
                    this.additionalOptionsTextBox.Text = string.Empty;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error on load additional options");

                this.additionalOptionsTextBox.Text = string.Empty;
            }

            try
            {
                bool isChecked = await this.userStorageSettingsService.GetIsAllProjectsEnabledAsync();

                this.allProjectsCheckBox.Checked = isChecked;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error on load is all projects enabled");

                this.allProjectsCheckBox.Checked = false;

                await this.userStorageSettingsService.SaveIsAllProjectsScanEnabledAsync(false);
            }

            this.CheckOptionConflicts();
        });
    }
}
