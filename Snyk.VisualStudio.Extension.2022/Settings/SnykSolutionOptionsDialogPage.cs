namespace Snyk.VisualStudio.Extension.Settings
{
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// Snyk dialog page for project Options.
    /// </summary>
    [Guid("6558dc66-aad3-41d6-84ed-8bea01fc852d")]
    [ComVisible(true)]
    public class SnykSolutionOptionsDialogPage : DialogPage
    {
        /// <summary>
        /// Gets a value indicating whether <see cref="SnykSolutionOptionsUserControl"/>.
        /// </summary>
        protected override IWin32Window Window => new SnykSolutionOptionsUserControl(SnykVSPackage.ServiceProvider);
    }
}
