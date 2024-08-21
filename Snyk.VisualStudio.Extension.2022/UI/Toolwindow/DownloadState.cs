using System.Windows;
using Microsoft.VisualStudio.Shell;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Encapsulate download state for tool window.
    /// </summary>
    public sealed class DownloadState : ToolWindowState
    {
        /// <summary>
        /// Gets a value indicating whether new <see cref="DownloadState"/>.
        /// </summary>
        public static DownloadState Instance { get; } = new DownloadState();

        /// <summary>
        /// Hide progress bar and download message components.
        /// </summary>
        public override void HideComponents()
        {
            this.ToolWindowControl.HideMainMessage();

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.ToolWindowControl.progressBar.Value = 0;
                this.ToolWindowControl.progressBarPanel.Visibility = Visibility.Collapsed;
                await this.ToolWindowControl.UpdateActionsStateAsync();
            });
        }

        /// <summary>
        /// Display progress bar and download message components.
        /// </summary>
        public override void DisplayComponents()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.ToolWindowControl.progressBar.Value = 0;
                this.ToolWindowControl.progressBarPanel.Visibility = Visibility.Visible;
                this.ToolWindowControl.DisplayMainMessage("Downloading latest Snyk CLI release 0%...");
                await this.ToolWindowControl.UpdateActionsStateAsync();
            });
        }
    }
}
