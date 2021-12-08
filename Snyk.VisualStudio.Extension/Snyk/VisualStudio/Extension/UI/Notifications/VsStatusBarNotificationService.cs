namespace Snyk.VisualStudio.Extension.UI.Notifications
{
    using Snyk.VisualStudio.Extension.Service;

    /// <summary>
    /// Display notifications in Visual Studio status bar.
    /// </summary>
    public class VsStatusBarNotificationService
    {
        private static VsStatusBarNotificationService instance;

        private VsStatusBar statusBar;

        private VsStatusBarNotificationService()
        {
        }

        /// <summary>
        /// Gets singleton instance of <see cref="VsStatusBarNotificationService"/>.
        /// </summary>
        public static VsStatusBarNotificationService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new VsStatusBarNotificationService();
                }

                return instance;
            }
        }

        /// <summary>
        /// Initialize event listeners for this service.
        /// </summary>
        /// <param name="serviceProvider">Service provider.</param>
        public void InitializeEventListeners(ISnykServiceProvider serviceProvider)
        {
            SnykTasksService tasksService = serviceProvider.TasksService;

            this.statusBar = VsStatusBar.Instance;

            tasksService.DownloadStarted += this.OnDownloadStarted;
            tasksService.DownloadFinished += this.OnDownloadFinished;
            tasksService.DownloadCancelled += this.OnDownloadCancelled;

            tasksService.ScanningCancelled += this.OnScanningCancelled;
            tasksService.CliScanningStarted += this.OnCliScanningStarted;
            tasksService.SnykCodeScanningStarted += this.OnSnykCodeScanningStarted;
            tasksService.OssScanningFinished += this.OnOssScanningFinished;
            tasksService.SnykCodeScanningFinished += this.OnSnykCodeScanningFinished;
        }

        public void ShowSnykCodeUpdateMessage(string message) => this.statusBar.ShowSnykCodeUpdateMessage(message);

        private void OnOssScanningFinished(object sender, SnykCliScanEventArgs eventArgs)
        {
            if (eventArgs.SnykCodeScanRunning)
            {
                return;
            }

            this.statusBar.ShowFinishedSearchMessage("Snyk scan finished");
        }

        private void OnSnykCodeScanningFinished(object sender, SnykCodeScanEventArgs eventArgs)
        {
            if (eventArgs.OssScanRunning)
            {
                return;
            }

            this.statusBar.ShowFinishedSearchMessage("Snyk scan finished");
        }

        private void OnCliScanningStarted(object sender, SnykCliScanEventArgs eventArgs)
            => this.statusBar.ShowStartSearchMessage("Snyk is scanning...");

        private void OnSnykCodeScanningStarted(object sender, SnykCodeScanEventArgs eventArgs)
            => this.statusBar.ShowStartSearchMessage("Snyk is scanning...");

        private void OnScanningCancelled(object sender, SnykCliScanEventArgs eventArgs)
            => this.statusBar.ShowFinishedSearchMessage("Snyk scan cancelled");

        private void OnDownloadFinished(object sender, SnykCliDownloadEventArgs eventArgs)
            => this.statusBar.ShowDownloadFinishedMessage("Snyk CLI downloaded successfully");

        private void OnDownloadStarted(object sender, SnykCliDownloadEventArgs eventArgs)
            => this.statusBar.ShowDownloadProgressMessage("Downloading latest Snyk CLI release...");

        private void OnDownloadCancelled(object sender, SnykCliDownloadEventArgs eventArgs)
            => this.statusBar.ShowDownloadFinishedMessage("Snyk CLI download cancelled");
    }
}
