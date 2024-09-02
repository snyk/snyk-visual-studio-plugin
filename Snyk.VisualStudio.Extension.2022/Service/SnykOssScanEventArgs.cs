using System;
using System.Collections.Generic;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Service.Domain;

namespace Snyk.VisualStudio.Extension.Service
{
    /// <summary>
    /// CLI scan event args.
    /// </summary>
    public class SnykOssScanEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykOssScanEventArgs"/> class.
        /// </summary>
        public SnykOssScanEventArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykOssScanEventArgs"/> class.
        /// </summary>
        /// <param name="ossError"><see cref="OssError"/> object.</param>
        /// <param name="featuresSettings">Features settings.</param>
        public SnykOssScanEventArgs(OssError ossError, FeaturesSettings featuresSettings)
        {
            this.Error = ossError;
            this.FeaturesSettings = featuresSettings;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykOssScanEventArgs"/> class.
        /// </summary>
        /// <param name="cliResult"><see cref="CliResult"/> object.</param>
        public SnykOssScanEventArgs(IDictionary<string, IEnumerable<Issue>> cliResult)
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
        /// Gets or sets a value indicating whether <see cref="OssError"/> object.
        /// </summary>
        public OssError Error { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="CliResult"/> object.
        /// </summary>
        public IDictionary<string, IEnumerable<Issue>> Result { get; set; }
    }
}
