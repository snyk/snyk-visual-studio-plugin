using System;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Serilog;
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
            if (this.serviceProvider.SolutionService.IsSolutionOpen())
            {
                string additionalOptions = this.additionalOptionsTextBox.Text;

                this.serviceProvider.SnykOptionsManager.SaveAdditionalOptionsAsync(additionalOptions).FireAndForget();
            }
        }

        private void SnykProjectOptionsUserControl_Load(object sender, EventArgs eventArgs) => ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            this.OnVisibleChanged(eventArgs);

            bool isProjectOpened = this.serviceProvider.SolutionService.IsSolutionOpen();

            this.additionalOptionsTextBox.Enabled = isProjectOpened;

            if (!isProjectOpened)
            {
                return;
            }

            try
            {
                string additionalOptions = await this.serviceProvider.SnykOptionsManager.GetAdditionalOptionsAsync();

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
        });
    }
}
