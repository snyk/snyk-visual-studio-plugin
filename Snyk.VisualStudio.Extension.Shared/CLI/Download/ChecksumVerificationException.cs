namespace Snyk.VisualStudio.Extension.Shared.CLI.Download
{
    using System;

    /// <summary>
    /// Exception for CLI download verification.
    /// </summary>
    public class ChecksumVerificationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChecksumVerificationException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public ChecksumVerificationException(string message)
            : base(message)
        {
        }

        public ChecksumVerificationException(string expectedHash, string receivedHash)
            : base($"Expected {expectedHash}, but downloaded file has {receivedHash}")
        {
        }
    }
}
