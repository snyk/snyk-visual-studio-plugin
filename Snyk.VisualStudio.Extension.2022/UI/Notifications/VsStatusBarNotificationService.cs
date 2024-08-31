namespace Snyk.VisualStudio.Extension.UI.Notifications
{
    using System;
    using Snyk.Code.Library.Service;
    using Snyk.Common.Settings;
    using Snyk.VisualStudio.Extension.Service;
    using Snyk.VisualStudio.Extension.Settings;

    /// <summary>
    /// Display notifications in Visual Studio status bar.
    /// </summary>
    public class VsStatusBarNotificationService
    {
        private static VsStatusBarNotificationService instance;

        private VsStatusBar statusBar;

        private ISnykOptions options;

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
            tasksService.DownloadFailed += this.OnDownloadFailed;

            tasksService.ScanningCancelled += this.OnScanningCancelled;
            tasksService.OssScanningStarted += this.OnOssScanningStarted;
            tasksService.SnykCodeScanningStarted += this.OnSnykCodeScanningStarted;
            tasksService.OssScanningFinished += this.OnOssScanningFinished;
            tasksService.SnykCodeScanningFinished += this.OnSnykCodeScanningFinished;

            tasksService.OssScanError += this.OnOssScanError;
            tasksService.SnykCodeScanError += this.OnSykCodeScanError;
        }

        /// <summary>
        /// Initialize SnykCode event listeners for this service.
        /// </summary>
        /// <param name="codeService">SnykCode service instance</param>
        /// <param name="options">Extension options.</param>
        public void InitializeEventListeners(ISnykCodeService codeService, ISnykOptions options)
        {
            codeService.ScanEventHandler += this.OnSnykCodeScanUpdate;

            this.options = options;
        }

        private void OnOssScanError(object sender, SnykOssScanEventArgs eventArgs)
        {
            if (this.options == null || this.statusBar == null)
            {
                return;
            }

            if (!this.options.SnykCodeSecurityEnabled && !this.options.SnykCodeQualityEnabled)
            {
                this.statusBar.ShowSnykCodeUpdateMessage("Snyk Open Source scan error");
            }
        }

        private void OnSykCodeScanError(object sender, SnykCodeScanEventArgs eventArgs)
        {
            if (!this.options.OssEnabled)
            {
                this.statusBar.ShowSnykCodeUpdateMessage("Snyk Code scan error");
            }
        }

        private void OnSnykCodeScanUpdate(object sender, SnykCodeEventArgs eventArgs)
            => this.statusBar.ShowSnykCodeUpdateMessage($"{eventArgs.ScanState} {eventArgs.Progress}%");

        private void OnOssScanningFinished(object sender, SnykOssScanEventArgs eventArgs)
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

        private void OnOssScanningStarted(object sender, SnykOssScanEventArgs eventArgs)
            => this.statusBar.ShowStartSearchMessage("Snyk is scanning...");

        private void OnSnykCodeScanningStarted(object sender, SnykCodeScanEventArgs eventArgs)
            => this.statusBar.ShowStartSearchMessage("Snyk is scanning...");

        private void OnScanningCancelled(object sender, SnykOssScanEventArgs eventArgs)
            => this.statusBar.ShowFinishedSearchMessage("Snyk scan cancelled");

        private void OnDownloadFinished(object sender, SnykCliDownloadEventArgs eventArgs)
            => this.statusBar.ShowDownloadFinishedMessage("Snyk CLI downloaded successfully");

        private void OnDownloadStarted(object sender, SnykCliDownloadEventArgs eventArgs)
            => this.statusBar.ShowDownloadProgressMessage("Downloading latest Snyk CLI release...");

        private void OnDownloadCancelled(object sender, SnykCliDownloadEventArgs eventArgs)
            => this.statusBar.ShowDownloadFinishedMessage("Snyk CLI download cancelled");

        private void OnDownloadFailed(object sender, Exception exception)
            => this.statusBar.ShowDownloadFinishedMessage("Snyk CLI download failed");
    }
}
