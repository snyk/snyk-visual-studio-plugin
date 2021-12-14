namespace Snyk.VisualStudio.Extension.Service
{
    using System;
    using Snyk.Code.Library.Domain.Analysis;

    /// <summary>
    /// CLI scan event args.
    /// </summary>
    public class SnykCodeScanEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeScanEventArgs"/> class.
        /// </summary>
        public SnykCodeScanEventArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeScanEventArgs"/> class.
        /// </summary>
        /// <param name="error">Error message.</param>
        public SnykCodeScanEventArgs(string error) => this.Error = error;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeScanEventArgs"/> class.
        /// </summary>
        /// <param name="analysisResult"><see cref="AnalysisResult"/> object.</param>
        public SnykCodeScanEventArgs(AnalysisResult analysisResult) => this.Result = analysisResult;

        /// <summary>
        /// Gets or sets a value indicating whether OSS scan still running or not.
        /// </summary>
        public bool OssScanRunning { get; set; }

        /// <summary>
        /// Gets or sets a value indicating error message.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="AnalysisResult"/> object.
        /// </summary>
        public AnalysisResult Result { get; set; }
    }
}
