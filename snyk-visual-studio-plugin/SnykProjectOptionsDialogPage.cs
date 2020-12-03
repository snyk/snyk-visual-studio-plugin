using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Services;
using Snyk.VisualStudio.Extension.UI;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Snyk.VisualStudio.Extension.Settings
{
    [Guid("6558dc66-aad3-41d6-84ed-8bea01fc852d")]
    public class SnykProjectOptionsDialogPage : DialogPage
    {
        protected override IWin32Window Window
        {
            get
            {
                var optionsUserControl = new SnykProjectOptionsUserControl(SnykSolutionService.Instance);
                optionsUserControl.projectOptionsPage = this;
                optionsUserControl.Initialize();

                return optionsUserControl;
            }
        }
    }
}
