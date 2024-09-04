using System;

namespace Snyk.VisualStudio.Extension.Download
{
    /// <summary>
    /// Exception for CLI download verification.
    /// </summary>
    public class ChecksumVerificationException : Exception
    {
        public string ExpectedHash { get; }
        public string ActualHash { get; }

        public ChecksumVerificationException(string expectedHash, string actualHash)
            : base($"Expected {expectedHash}, but downloaded file has {actualHash}")
        {
            this.ExpectedHash = expectedHash;
            this.ActualHash = actualHash;
        }
    }
}
