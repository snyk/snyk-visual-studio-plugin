namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    using System.Windows;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// Implement Run scan state for tool window.
    /// </summary>
    public class RunScanState : ToolWindowState
    {
        /// <summary>
        /// Gets a value indicating whether new <see cref="RunScanState"/> object.
        /// </summary>
        public static RunScanState Instance => new RunScanState();

        /// <summary>
        /// Hide run scan message and disable stop actions.
        /// </summary>
        public override void HideComponents()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.ToolWindowControl.messagePanel.Visibility = Visibility.Collapsed;
            });
        }

        /// <summary>
        /// Display run scan message and enable stop actions.
        /// </summary>
        public override void DisplayComponents()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.ToolWindowControl.messagePanel.Visibility = Visibility.Visible;

                this.ToolWindowControl.messagePanel.ShowRunScanMessage();
            });
        }
    }
}
