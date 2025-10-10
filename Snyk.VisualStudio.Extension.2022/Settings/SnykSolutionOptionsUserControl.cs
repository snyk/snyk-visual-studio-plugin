using System;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.VisualStudio.Extension;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Solution settings control.
    /// </summary>
    public partial class SnykSolutionOptionsUserControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykSolutionOptionsUserControl>();

        private readonly ISnykServiceProvider serviceProvider;
        public string AdditionalOptions { get; set; }
        public string Organization { get; set; }
        public bool AutoOrganization { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykSolutionOptionsUserControl"/> class.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider.</param>
        public SnykSolutionOptionsUserControl(ISnykServiceProvider serviceProvider)
        {
            this.InitializeComponent();

            this.serviceProvider = serviceProvider;
        }

        private void AdditionalOptionsTextBox_TextChanged(object sender, EventArgs e)
        {
            AdditionalOptions = this.additionalOptionsTextBox.Text;
            // Only save to storage, Language Server notification will happen on dialog save
            try
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await this.serviceProvider.SnykOptionsManager.SaveAdditionalOptionsAsync(AdditionalOptions);
                    Logger.Information("Additional options saved: {AdditionalOptions}", AdditionalOptions);
                }).FireAndForget();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save additional options");
            }
        }

        private void OrganizationTextBox_TextChanged(object sender, EventArgs e)
        {
            Organization = this.organizationTextBox.Text;
            // Only save to storage, Language Server notification will happen on dialog save
            try
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await this.serviceProvider.SnykOptionsManager.SaveOrganizationAsync(Organization);
                    Logger.Information("Organization saved: {Organization}", Organization);
                }).FireAndForget();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save organization");
            }
        }

        private void AutoOrganizationCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            AutoOrganization = this.autoOrganizationCheckBox.Checked;
            // Only save to storage, Language Server notification will happen on dialog save
            try
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await this.serviceProvider.SnykOptionsManager.SaveAutoOrganizationAsync(AutoOrganization);
                    Logger.Information("Auto organization saved: {AutoOrganization}", AutoOrganization);
                }).FireAndForget();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save auto organization");
            }
        }

        private void WebAccountSettingsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.WebAccountSettingsLabel.LinkVisited = true;
            System.Diagnostics.Process.Start("https://app.snyk.io/account");
        }

        private void SnykProjectOptionsUserControl_Load(object sender, EventArgs eventArgs) => ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            // Load additional options
            try
            {
                var additionalOptions = await this.serviceProvider.SnykOptionsManager.GetAdditionalOptionsAsync();

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

            // Load organization
            try
            {
                var organization = await this.serviceProvider.SnykOptionsManager.GetOrganizationAsync();

                if (!string.IsNullOrEmpty(organization))
                {
                    this.organizationTextBox.Text = organization;
                }
                else
                {
                    this.organizationTextBox.Text = string.Empty;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error on load organization");
                this.organizationTextBox.Text = string.Empty;
            }

            // Load auto organization
            try
            {
                var autoOrganization = await this.serviceProvider.SnykOptionsManager.GetAutoOrganizationAsync();
                this.autoOrganizationCheckBox.Checked = autoOrganization;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error on load auto organization");
                this.autoOrganizationCheckBox.Checked = true; // Default to true
            }
        });
    }
}
