namespace Snyk.VisualStudio.Extension.Service
{
    using System;
    using Snyk.VisualStudio.Extension.CLI;

     /// <summary>
     /// Exception for OSS scan errors.
     /// </summary>
    public class OssScanException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OssScanException"/> class.
        /// </summary>
        public OssScanException()
            : base()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether cli error with details.
        /// </summary>
        public CliError Error { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether cli run process is cancelled.
        /// </summary>
        public bool IsCancelled { get; set; }
    }
}
