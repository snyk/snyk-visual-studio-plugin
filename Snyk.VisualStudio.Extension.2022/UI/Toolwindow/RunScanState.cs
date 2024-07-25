using System.Windows;
using Microsoft.VisualStudio.Shell;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Implement Run scan state for tool window.
    /// </summary>
    public class RunScanState : ToolWindowState
    {
        /// <summary>
        /// Gets a value indicating whether new <see cref="RunScanState"/> object.
        /// </summary>
        public static RunScanState Instance { get; } = new RunScanState();

        /// <summary>
        /// Hide run scan message and disable stop actions.
        /// </summary>
        public override void HideComponents()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.ToolWindowControl.messagePanel.Visibility = Visibility.Collapsed;

                await this.ToolWindowControl.UpdateActionsStateAsync();
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

                await this.ToolWindowControl.UpdateActionsStateAsync();
            });
        }
    }
}
