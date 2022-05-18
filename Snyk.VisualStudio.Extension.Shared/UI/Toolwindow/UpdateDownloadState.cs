namespace Snyk.VisualStudio.Extension.Shared.UI.Toolwindow
{
    using System.Windows;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// Incapsulate Update donwload state for toolwindow.
    /// </summary>
    public class UpdateDownloadState : ToolWindowState
    {
        /// <summary>
        /// Gets a value indicating whether new <see cref="UpdateDownloadState"/>.
        /// </summary>
        public static UpdateDownloadState Instance => new UpdateDownloadState();

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
                this.ToolWindowControl.DisplayMainMessage("Updating the Snyk CLI to the latest release 0%...");
                await this.ToolWindowControl.UpdateActionsStateAsync();
            });
        }
    }
}
