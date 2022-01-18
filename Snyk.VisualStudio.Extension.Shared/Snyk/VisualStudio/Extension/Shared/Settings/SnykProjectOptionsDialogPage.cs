namespace Snyk.VisualStudio.Extension.Shared.Settings
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// Snyk dialog page for project Options.
    /// </summary>
    [Guid("6558dc66-aad3-41d6-84ed-8bea01fc852d")]
    [ComVisible(true)]
    public class SnykProjectOptionsDialogPage : DialogPage
    {
        /// <summary>
        /// Gets a value indicating whether <see cref="SnykProjectOptionsUserControl"/>.
        /// </summary>
        protected override IWin32Window Window => new SnykProjectOptionsUserControl(SnykVSPackage.ServiceProvider);
    }
}
