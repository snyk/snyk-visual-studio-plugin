namespace Snyk.VisualStudio.Extension.Service
{
    using System;

    /// <summary>
    /// CLI download event args.
    /// </summary>
    public class SnykCliDownloadEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCliDownloadEventArgs"/> class.
        /// </summary>
        public SnykCliDownloadEventArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCliDownloadEventArgs"/> class.
        /// </summary>
        /// <param name="progress">CLI download progress (from 0 to 100%).</param>
        public SnykCliDownloadEventArgs(int progress) => this.Progress = progress;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCliDownloadEventArgs"/> class.
        /// </summary>
        /// <param name="message">CLI download message.</param>
        public SnykCliDownloadEventArgs(string message) => this.Message = message;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCliDownloadEventArgs"/> class.
        /// </summary>
        /// <param name="isUpdateDownload">Is this download is for update.</param>
        public SnykCliDownloadEventArgs(bool isUpdateDownload) => this.IsUpdateDownload = isUpdateDownload;

        /// <summary>
        /// Gets or sets a value indicating whether progress.
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is update download.
        /// </summary>
        public bool IsUpdateDownload { get; set; }
    }
}
