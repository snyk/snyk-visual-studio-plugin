namespace Snyk.VisualStudio.Extension.Service
{
    using System.Threading;

    /// <summary>
    /// Progress woker interface to use in low level APIs.
    /// </summary>
    public class SnykProgressWorker : ISnykProgressWorker
    {
        /// <summary>
        /// Gets or sets a value indicating whether token source.
        /// </summary>
        public CancellationTokenSource TokenSource { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tasks service.
        /// </summary>
        public SnykTasksService TasksService { get; set; }

        /// <summary>
        /// Notify progress update.
        /// </summary>
        /// <param name="progress">Current progress from 1 to 100.</param>
        public void UpdateProgress(int progress) => this.TasksService.OnDownloadUpdate(progress);

        /// <summary>
        /// Notify download finished.
        /// </summary>
        public void DownloadFinished() => this.TasksService.OnDownloadFinished();

        /// <summary>
        /// Notify cancel if cancellation requested by user.
        /// </summary>
        public void CancelIfCancellationRequested()
        {
            if (this.TokenSource.Token.IsCancellationRequested)
            {
                this.TokenSource.Token.ThrowIfCancellationRequested();
            }
        }

        /// <summary>
        /// Notify download started.
        /// </summary>
        public void DownloadStarted() => this.TasksService.OnDownloadStarted();

        /// <summary>
        /// Notify donwload cancelled.
        /// </summary>
        /// <param name="message">Cancelled message.</param>
        public void DownloadCancelled(string message) => this.TasksService.OnDownloadCancelled(message);
    }
}
