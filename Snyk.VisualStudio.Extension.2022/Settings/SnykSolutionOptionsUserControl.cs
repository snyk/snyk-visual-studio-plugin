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

        /// <summary>
        /// Gets the current state of the auto organization checkbox.
        /// </summary>
        public bool IsAutoOrganizationChecked => this.autoOrganizationCheckBox.Checked;
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
            var isAutoMode = this.autoOrganizationCheckBox.Checked;
            
            // Implement IntelliJ logic for checkbox behavior
            try
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    // When checkbox is ticked (auto mode):
                    // - orgSetByUser is set to false
                    // - preferredOrg is set to ""
                    // - preferredOrgTextField is set to the value of autoDeterminedOrg
                    if (isAutoMode)
                    {
                        await this.serviceProvider.SnykOptionsManager.SaveOrgSetByUserAsync(false);
                        await this.serviceProvider.SnykOptionsManager.SavePreferredOrgAsync("");
                        
                        // Update text field to show auto-determined org
                        var autoDeterminedOrg = await this.serviceProvider.SnykOptionsManager.GetAutoDeterminedOrgAsync();
                        this.organizationTextBox.Text = autoDeterminedOrg;
                        this.Organization = autoDeterminedOrg;
                    }
                    else
                    {
                        // When checkbox is unticked (manual mode):
                        // - preferredOrgTextField is set to ""
                        // - orgSetByUser will be set to true when apply is clicked
                        this.organizationTextBox.Text = "";
                        this.Organization = "";
                    }
                    
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

            // Load organization settings with IntelliJ logic
            try
            {
                var orgSetByUser = await this.serviceProvider.SnykOptionsManager.GetOrgSetByUserAsync();
                
                // Set checkbox state based on IntelliJ logic
                // Checkbox should be ticked if orgSetByUser is false (auto mode)
                this.autoOrganizationCheckBox.Checked = !orgSetByUser;

                // Populate text field based on IntelliJ logic
                if (!orgSetByUser)
                {
                    // Show autoDeterminedOrg if orgSetByUser is false
                    var autoDeterminedOrg = await this.serviceProvider.SnykOptionsManager.GetAutoDeterminedOrgAsync();
                    
                    // If autoDeterminedOrg is empty, fallback to global organization
                    if (string.IsNullOrEmpty(autoDeterminedOrg))
                    {
                        var globalOrg = this.serviceProvider.Options.Organization;
                        this.organizationTextBox.Text = globalOrg ?? string.Empty;
                        this.Organization = globalOrg ?? string.Empty;
                    }
                    else
                    {
                        this.organizationTextBox.Text = autoDeterminedOrg;
                        this.Organization = autoDeterminedOrg;
                    }
                }
                else
                {
                    // Show preferredOrg if orgSetByUser is true
                    var preferredOrg = await this.serviceProvider.SnykOptionsManager.GetPreferredOrgAsync();
                    this.organizationTextBox.Text = preferredOrg ?? string.Empty;
                    this.Organization = preferredOrg ?? string.Empty;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error on load organization settings");
                this.autoOrganizationCheckBox.Checked = true; // Default to auto mode
                this.organizationTextBox.Text = string.Empty;
            }
        });
    }
}
