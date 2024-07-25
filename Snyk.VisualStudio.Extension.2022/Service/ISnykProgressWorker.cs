﻿namespace Snyk.VisualStudio.Extension.Shared.Service
{
    /// <summary>
    /// Progress woker interface to use in low level APIs.
    /// </summary>
    public interface ISnykProgressWorker
    {
        /// <summary>
        /// Gets or sets a value indicating whether is work finished.
        /// </summary>
        bool IsWorkFinished { get; set; }

        /// <summary>
        /// Notify download started.
        /// </summary>
        void DownloadStarted();

        /// <summary>
        /// Notify progress update.
        /// </summary>
        /// <param name="progress">Current progress from 1 to 100.</param>
        void UpdateProgress(int progress);

        /// <summary>
        /// Notify download finished.
        /// </summary>
        void DownloadFinished();

        /// <summary>
        /// Notify cancel if cancellation requested by user.
        /// </summary>
        void CancelIfCancellationRequested();

        /// <summary>
        /// Notify download cancelled.
        /// </summary>
        /// <param name="message">Cancelled message.</param>
        void DownloadCancelled(string message);
    }
}
