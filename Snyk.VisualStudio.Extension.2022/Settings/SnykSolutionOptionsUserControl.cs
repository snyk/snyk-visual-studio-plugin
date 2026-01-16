// ABOUTME: This file implements the WinForms user control for solution-specific settings
// ABOUTME: It provides UI for configuring per-solution additional parameters and organization settings
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
    public partial class SnykSolutionOptionsUserControl : BaseSnykUserControl
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykSolutionOptionsUserControl>();

        public string AdditionalOptions { get; set; }
        public string Organization { get; set; }

        /// <summary>
        /// Gets the current state of the auto organization checkbox.
        /// </summary>
        public bool IsAutoOrganizationChecked => this.autoOrganizationCheckBox.Checked;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykSolutionOptionsUserControl"/> class.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider.</param>
        public SnykSolutionOptionsUserControl(ISnykServiceProvider serviceProvider) : base(serviceProvider)
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Loads solution-specific settings asynchronously.
        /// Overrides base class to provide async loading for solution-specific storage.
        /// </summary>
        protected override async System.Threading.Tasks.Task UpdateViewFromOptionsAsync()
        {
            // Load additional options
            try
            {
                var additionalOptions = await this.serviceProvider.SnykOptionsManager.GetAdditionalOptionsAsync() ?? string.Empty;
                this.additionalOptionsTextBox.Text = additionalOptions;
                this.AdditionalOptions = additionalOptions;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error on load additional options");
                this.additionalOptionsTextBox.Text = string.Empty;
            }

            // Load organization settings
            try
            {
                var orgSetByUser = await this.serviceProvider.SnykOptionsManager.GetOrgSetByUserAsync();

                // Checkbox should be ticked if orgSetByUser is false (auto mode)
                this.autoOrganizationCheckBox.Checked = !orgSetByUser;

                if (!orgSetByUser)
                {
                    // Show autoDeterminedOrg if orgSetByUser is false
                    var autoDeterminedOrg = await this.serviceProvider.SnykOptionsManager.GetAutoDeterminedOrgAsync() ?? string.Empty;
                    this.organizationTextBox.Text = autoDeterminedOrg;
                    this.Organization = autoDeterminedOrg;
                }
                else
                {
                    // Show preferredOrg if orgSetByUser is true
                    var preferredOrg = await this.serviceProvider.SnykOptionsManager.GetPreferredOrgAsync() ?? string.Empty;
                    this.organizationTextBox.Text = preferredOrg;
                    this.Organization = preferredOrg;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error on load organization settings");
                this.autoOrganizationCheckBox.Checked = true; // Default to auto mode
                this.organizationTextBox.Text = string.Empty;
            }
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
            // Save to preferredOrg immediately if in manual mode (consistent with AdditionalOptions behavior)
            // Only save if checkbox is unchecked (manual mode), otherwise wait for Apply/OK
            if (!this.autoOrganizationCheckBox.Checked)
            {
                try
                {
                    ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await this.serviceProvider.SnykOptionsManager.SavePreferredOrgAsync(Organization);
                        Logger.Information("Preferred organization saved: {Organization}", Organization);
                    }).FireAndForget();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to save preferred organization");
                }
            }
        }

        private void AutoOrganizationCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            var isAutoMode = this.autoOrganizationCheckBox.Checked;
            
            try
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    // When checkbox is ticked (auto mode):
                    // - orgSetByUser is set to false
                    // - preferredOrgTextField is set to the value of autoDeterminedOrg
                    if (isAutoMode)
                    {
                        await this.serviceProvider.SnykOptionsManager.SaveOrgSetByUserAsync(false);
                        // Update text field to show auto-determined org
                        var autoDeterminedOrg = await this.serviceProvider.SnykOptionsManager.GetAutoDeterminedOrgAsync();
                        this.organizationTextBox.Text = autoDeterminedOrg;
                        this.Organization = autoDeterminedOrg;
                    }
                    else
                    {
                        // When checkbox is unticked (manual mode):
                        // - orgSetByUser is set to true
                        // - preferredOrgTextField is set to the previously saved value.
                        await this.serviceProvider.SnykOptionsManager.SaveOrgSetByUserAsync(true);
                        var preferredOrg = await this.serviceProvider.SnykOptionsManager.GetPreferredOrgAsync();
                        this.organizationTextBox.Text = preferredOrg;
                        this.Organization = preferredOrg;
                    }

                    // Disable the Preferred Organization field if Auto-select Organization is enabled.
                    this.organizationTextBox.Enabled = !isAutoMode;
                    
                    Logger.Information("Auto organization state updated: {IsAutoMode}", isAutoMode);
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

        private void SnykProjectOptionsUserControl_Load(object sender, EventArgs eventArgs) =>
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await UpdateViewFromOptionsAsync();
            }).FireAndForget();
    }
}
