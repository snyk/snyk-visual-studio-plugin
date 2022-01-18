namespace Snyk.VisualStudio.Extension.Shared.UI.Toolwindow
{
    using System.Windows;

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

            this.ToolWindowControl.Dispatcher.Invoke(() =>
            {
                this.ToolWindowControl.progressBar.Value = 0;

                this.ToolWindowControl.progressBarPanel.Visibility = Visibility.Collapsed;
            });

            this.ToolWindowControl.UpdateActionsState();
        }

        /// <summary>
        /// Display progress bar and download message components.
        /// </summary>
        public override void DisplayComponents()
        {
            this.ToolWindowControl.Dispatcher.Invoke(() =>
            {
                this.ToolWindowControl.progressBar.Value = 0;

                this.ToolWindowControl.progressBarPanel.Visibility = Visibility.Visible;
            });

            this.ToolWindowControl.DisplayMainMessage("Updating the Snyk CLI to the latest release 0%...");

            this.ToolWindowControl.UpdateActionsState();
        }
    }
}
