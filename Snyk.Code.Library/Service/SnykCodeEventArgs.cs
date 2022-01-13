namespace Snyk.Code.Library.Service
{
    using System;

    /// <summary>
    /// SnykCode event args.
    /// </summary>
    public class SnykCodeEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets current scan state (step).
        /// </summary>
        public SnykCodeScanState ScanState { get; set; }

        /// <summary>
        /// Gets or sets current progress.
        /// </summary>
        public int Progress { get; set; }
    }
}
