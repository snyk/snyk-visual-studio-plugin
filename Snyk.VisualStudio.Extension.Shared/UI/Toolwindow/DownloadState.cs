namespace Snyk.VisualStudio.Extension.Shared.UI.Toolwindow
{
    using System.Windows;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// Encapsulate download state for tool window.
    /// </summary>
    public sealed class DownloadState : ToolWindowState
    {
        /// <summary>
        /// Gets a value indicating whether new <see cref="DownloadState"/>.
        /// </summary>
        public static DownloadState Instance => new DownloadState();

        /// <summary>
        /// Hide progress bar and download message components.
        /// </summary>
        public override void HideComponents()
        {
            this.ToolWindowControl.HideMainMessage();

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
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
                this.ToolWindowControl.progressBar.Value = 0;
                this.ToolWindowControl.progressBarPanel.Visibility = Visibility.Visible;
                this.ToolWindowControl.DisplayMainMessage("Downloading latest Snyk CLI release 0%...");
                await this.ToolWindowControl.UpdateActionsStateAsync();
            });
        }
    }
}
