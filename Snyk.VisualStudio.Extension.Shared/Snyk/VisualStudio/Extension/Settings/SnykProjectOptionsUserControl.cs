namespace Snyk.VisualStudio.Extension.Shared.Settings
{
    using System;
    using System.Windows.Forms;
    using Serilog;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.Service;

    /// <summary>
    /// Project settings control.
    /// </summary>
    public partial class SnykProjectOptionsUserControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykProjectOptionsUserControl>();

        private ISnykServiceProvider serviceProvider;

        private SnykUserStorageSettingsService userStorageSettingsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykProjectOptionsUserControl"/> class.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider.</param>
        public SnykProjectOptionsUserControl(ISnykServiceProvider serviceProvider)
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
            if (this.serviceProvider.SolutionService.IsSolutionOpen)
            {
                this.userStorageSettingsService.SaveAdditionalOptions(this.additionalOptionsTextBox.Text.ToString());

                this.CheckOptionConflicts();
            }
        }

        private void AllProjectsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.serviceProvider.SolutionService.IsSolutionOpen)
            {
                this.userStorageSettingsService.SaveIsAllProjectsScanEnabled(this.allProjectsCheckBox.Checked);

                this.CheckOptionConflicts();
            }
        }

        private void SnykProjectOptionsUserControl_Load(object sender, EventArgs eventArgs)
        {
            this.OnVisibleChanged(eventArgs);

            bool isProjectOpened = this.serviceProvider.SolutionService.IsSolutionOpen;

            this.additionalOptionsTextBox.Enabled = isProjectOpened;
            this.allProjectsCheckBox.Enabled = isProjectOpened;

            if (!isProjectOpened)
            {
                return;
            }

            try
            {
                string additionalOptions = this.userStorageSettingsService.GetAdditionalOptions();

                if (!string.IsNullOrEmpty(additionalOptions))
                {
                    this.additionalOptionsTextBox.Text = additionalOptions;
                }
                else
                {
                    this.additionalOptionsTextBox.Text = string.Empty;
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message);

                this.additionalOptionsTextBox.Text = string.Empty;
            }

            try
            {
                bool isChecked = this.userStorageSettingsService.GetIsAllProjectsEnabled();

                this.allProjectsCheckBox.Checked = isChecked;
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message);

                this.allProjectsCheckBox.Checked = false;

                this.userStorageSettingsService.SaveIsAllProjectsScanEnabled(false);
            }

            this.CheckOptionConflicts();
        }
    }
}
