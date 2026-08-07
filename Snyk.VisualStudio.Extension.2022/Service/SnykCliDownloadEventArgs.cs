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

        /// <summary>
        /// Whether a binary was actually fetched. False when the CLI on disk was already current:
        /// DownloadFinished is raised on that path too, because it is what starts the language server
        /// and clears the tool window's loading state, but nothing was downloaded and subscribers that
        /// report progress to the user must not claim otherwise.
        /// </summary>
        public bool BinaryWasDownloaded { get; set; } = true;

        /// <summary>
        /// Whether this CLI check was requested because the user changed a CLI setting, as opposed to
        /// being part of startup. Deliberately the reason rather than a required action: the language
        /// server subscriber uses it to decide whether a server that is already serving has to be moved
        /// onto the newly configured executable, and the status bar uses it to describe what happened.
        /// Naming it after either one of those would mislead the other.
        /// <para>
        /// Set as a property rather than through a constructor: the <c>bool</c> constructor overload
        /// already means <see cref="IsUpdateDownload"/>.
        /// </para>
        /// </summary>
        public bool CliSettingsChanged { get; set; }
    }
}
