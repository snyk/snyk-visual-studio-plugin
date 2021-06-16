namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    using System.Windows;

    /// <summary>
    /// Implements Scanning state for tool window.
    /// </summary>
    public class ScanningState : ToolWindowState
    {
        /// <summary>
        /// Gets a value indicating whether new <see cref="ScanningState"/> object.
        /// </summary>
        public static ScanningState Instance => new ScanningState();

        /// <summary>
        /// Hide main message and progress bar.
        /// </summary>
        public override void HideComponents()
        {
            this.ToolWindowControl.HideMainMessage();

            this.ToolWindowControl.Dispatcher.Invoke(() =>
            {
                this.ToolWindowControl.progressBar.Value = 0;
                this.ToolWindowControl.progressBar.IsIndeterminate = false;

                this.ToolWindowControl.progressBarPanel.Visibility = Visibility.Collapsed;
            });
        }

        /// <summary>
        /// Display main message and progress bar.
        /// </summary>
        public override void DisplayComponents()
        {
            this.ToolWindowControl.DisplayMainMessage("Scanning project for vulnerabilities...");

            this.ToolWindowControl.Dispatcher.Invoke(() =>
            {
                this.ToolWindowControl.progressBar.Value = 0;
                this.ToolWindowControl.progressBar.IsIndeterminate = true;

                this.ToolWindowControl.progressBarPanel.Visibility = Visibility.Visible;
            });
        }
    }
}
