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
            // Notify Language Server of configuration change
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await this.serviceProvider.LanguageClientManager.DidChangeConfigurationAsync(SnykVSPackage.Instance.DisposalToken);
            }).FireAndForget();
        }

        private void OrganizationTextBox_TextChanged(object sender, EventArgs e)
        {
            Organization = this.organizationTextBox.Text;
            // Notify Language Server of configuration change
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await this.serviceProvider.LanguageClientManager.DidChangeConfigurationAsync(SnykVSPackage.Instance.DisposalToken);
            }).FireAndForget();
        }

        private void OrganizationInfoLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.OrganizationInfoLink.LinkVisited = true;
            System.Diagnostics.Process.Start("https://docs.snyk.io/ide-tools/visual-studio-extension#organization-setting");
        }

        private void SnykProjectOptionsUserControl_Load(object sender, EventArgs eventArgs) => ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            try
            {
                var additionalOptions = await this.serviceProvider.SnykOptionsManager.GetAdditionalOptionsAsync();
                var organization = await this.serviceProvider.SnykOptionsManager.GetOrganizationAsync();

                if (!string.IsNullOrEmpty(additionalOptions))
                {
                    this.additionalOptionsTextBox.Text = additionalOptions;
                }
                else
                {
                    this.additionalOptionsTextBox.Text = string.Empty;
                }

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
                Logger.Error(e, "Error on load additional options and organization");

                this.additionalOptionsTextBox.Text = string.Empty;
                this.organizationTextBox.Text = string.Empty;
            }
        });
    }
}
