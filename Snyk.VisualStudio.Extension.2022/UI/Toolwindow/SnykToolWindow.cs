using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("b38c6cbc-524d-4f30-8a18-936e3104b734")]
    public class SnykToolWindow : ToolWindowPane
    {
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykToolWindow"/> class.
        /// </summary>
        public SnykToolWindow()
            : this(createContent: true)
        {
        }

        // Test seam: createContent:false skips building the WPF control (which requires a WebView2
        // runtime), so the Content-disposal chaining contract can be exercised with an injected
        // IDisposable in unit tests.
        internal SnykToolWindow(bool createContent)
            : base(null)
        {
            this.Caption = "Snyk";

            if (createContent)
            {
                this.Content = new SnykToolWindowControl(this);
            }

            this.ToolBar = new CommandID(SnykGuids.SnykVSPackageCommandSet, SnykGuids.SnykToolbarId);

            this.ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;
        }

        // WindowPane.Dispose (the base of ToolWindowPane) does NOT chain Dispose into the Content
        // control, so the WebView2-hosting child panels — and their msedgewebview2.exe processes +
        // %LOCALAPPDATA%\Snyk\WebView2 user-data folders — would leak when the tool window is
        // destroyed. Dispose Content explicitly.
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    this.DisposeContentOnce();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        // internal for testability (InternalsVisibleTo): idempotent so a double Dispose disposes
        // the Content panels exactly once.
        internal void DisposeContentOnce()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            (this.Content as IDisposable)?.Dispose();
        }

        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var toolWindowControl = Content as SnykToolWindowControl;
                if (toolWindowControl == null) return;
                var package = Package as SnykVSPackage;
                if (package == null) return;
                package.ToolWindow = this;
                package.ToolWindowControl = toolWindowControl;
                var serviceProvider = await package.GetServiceAsync(typeof(SnykService)) as SnykService ??
                                      throw new InvalidOperationException("Could not find Snyk Service");
                toolWindowControl.InitializeEventListeners(serviceProvider);
                toolWindowControl.Initialize(serviceProvider);
            });

        }

        // Search/severity-filter UI removed: the LS-rendered HTML issue tree provides its own
        // filter toolbar (snyk.toggleTreeFilter), so the native tool-window search box and
        // severity filters are no longer used. SearchEnabled defaults to false in the base class.
    }
}
