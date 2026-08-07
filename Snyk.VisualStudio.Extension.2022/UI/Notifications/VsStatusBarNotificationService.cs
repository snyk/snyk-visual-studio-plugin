using System;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.UI.Notifications
{
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
            var tasksService = serviceProvider.TasksService;

            this.statusBar = VsStatusBar.Instance;

            tasksService.DownloadStarted += this.OnDownloadStarted;
            tasksService.DownloadFinished += this.OnDownloadFinished;
            tasksService.CliDownloadDeclined += this.OnCliDownloadDeclined;
            tasksService.CliDownloadAborted += this.OnCliDownloadAborted;
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
        /// <param name="options">Extension options.</param>
        public void InitializeEventListeners(ISnykOptions options)
        {
            this.options = options;
        }

        private void OnOssScanError(object sender, SnykOssScanEventArgs eventArgs)
        {
            if (this.options == null || this.statusBar == null)
            {
                return;
            }

            if (!this.options.SnykCodeSecurityEnabled)
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
        {
            // Raised on the nothing-to-download path too, so the message has to distinguish the two —
            // but both must still be reported. Returning early here left the "Downloading latest Snyk
            // CLI release..." text and its spinning icon up for the rest of the session whenever
            // DownloadStarted had already fired and the checksum check then found the binary current,
            // which is the common case on every startup after the first. ShowDownloadFinishedMessage is
            // what clears the animation, and calling it where nothing was animating is a no-op.
            this.statusBar.ShowDownloadFinishedMessage(
                eventArgs.BinaryWasDownloaded
                    ? "Snyk CLI downloaded successfully"
                    : "Snyk CLI is up to date");
        }

        private void OnDownloadStarted(object sender, SnykCliDownloadEventArgs eventArgs)
            => this.statusBar.ShowDownloadProgressMessage("Downloading latest Snyk CLI release...");

        // Declined means automatic management is off, so nothing was ever going to be fetched. At startup
        // that is the steady state and needs no announcement — and there is no download animation to clear,
        // because none was started. Only a settings change the user just made is worth confirming.
        private void OnCliDownloadDeclined(object sender, SnykCliDownloadEventArgs eventArgs)
        {
            if (eventArgs?.CliSettingsChanged == true)
            {
                this.statusBar.ShowDownloadFinishedMessage("Snyk CLI settings applied");
            }
        }

        // A download really was in progress and really was cancelled, so say so — whatever prompted it.
        private void OnCliDownloadAborted(object sender, SnykCliDownloadEventArgs eventArgs)
            => this.statusBar.ShowDownloadFinishedMessage("Snyk CLI download cancelled");

        private void OnDownloadFailed(object sender, Exception exception)
            => this.statusBar.ShowDownloadFinishedMessage("Snyk CLI download failed");
    }
}
