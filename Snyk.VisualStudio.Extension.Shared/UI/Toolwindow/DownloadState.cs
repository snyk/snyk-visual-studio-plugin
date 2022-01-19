namespace Snyk.VisualStudio.Extension.Shared.UI.Toolwindow
{
    using System.Windows;

    /// <summary>
    /// Incapsulate Donwload state for toolwindow.
    /// </summary>
    public class DownloadState : ToolWindowState
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

            this.ToolWindowControl.DisplayMainMessage("Downloading latest Snyk CLI release 0%...");

            this.ToolWindowControl.UpdateActionsState();
        }
    }
}
