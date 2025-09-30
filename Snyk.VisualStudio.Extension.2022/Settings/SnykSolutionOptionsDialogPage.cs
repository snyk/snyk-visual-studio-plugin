using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Snyk dialog page for project Options.
    /// </summary>
    [Guid("6558dc66-aad3-41d6-84ed-8bea01fc852d")]
    [ComVisible(true)]
    public class SnykSolutionOptionsDialogPage : DialogPage, ISnykSolutionOptionsDialogPage
    {
        public void Initialize(ISnykServiceProvider provider)
        {
            this.serviceProvider = provider;
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="SnykSolutionOptionsUserControl"/>.
        /// </summary>
        protected override IWin32Window Window => SnykSolutionOptionsUserControl;

        private SnykSolutionOptionsUserControl snykSolutionOptionsUserControl;
        private ISnykServiceProvider serviceProvider;

        public SnykSolutionOptionsUserControl SnykSolutionOptionsUserControl
        {
            get
            {
                if (snykSolutionOptionsUserControl == null)
                {
                    snykSolutionOptionsUserControl = new SnykSolutionOptionsUserControl(serviceProvider);
                }
                return snykSolutionOptionsUserControl;
            }
        }

        public override void SaveSettingsToStorage()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await this.serviceProvider.SnykOptionsManager.SaveOrganizationAsync(SnykSolutionOptionsUserControl.Organization);
                await this.serviceProvider.SnykOptionsManager.SaveAdditionalOptionsAsync(SnykSolutionOptionsUserControl.AdditionalOptions);
            }).FireAndForget();
        }

        protected override void OnClosed(EventArgs e)
        {
            // do nothing
        }
    }
}
