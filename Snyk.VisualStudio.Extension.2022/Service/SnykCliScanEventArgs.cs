namespace Snyk.VisualStudio.Extension.Service
{
    using System;
    using Snyk.VisualStudio.Extension.CLI;
    using Snyk.VisualStudio.Extension.Service.Domain;

    /// <summary>
    /// CLI scan event args.
    /// </summary>
    public class SnykCliScanEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCliScanEventArgs"/> class.
        /// </summary>
        public SnykCliScanEventArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCliScanEventArgs"/> class.
        /// </summary>
        /// <param name="cliError"><see cref="CliError"/> object.</param>
        /// <param name="featuresSettings">Features settings.</param>
        public SnykCliScanEventArgs(CliError cliError, FeaturesSettings featuresSettings)
        {
            this.Error = cliError;
            this.FeaturesSettings = featuresSettings;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCliScanEventArgs"/> class.
        /// </summary>
        /// <param name="cliResult"><see cref="CliResult"/> object.</param>
        public SnykCliScanEventArgs(CliResult cliResult)
        {
            this.Result = cliResult;
        }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="FeaturesSettings"/> object.
        /// </summary>
        public FeaturesSettings FeaturesSettings { get; }

        /// <summary>
        /// Gets or sets a value indicating whether SnykCode scan still running or not.
        /// </summary>
        public bool SnykCodeScanRunning { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="CliError"/> object.
        /// </summary>
        public CliError Error { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="CliResult"/> object.
        /// </summary>
        public CliResult Result { get; set; }
    }
}
