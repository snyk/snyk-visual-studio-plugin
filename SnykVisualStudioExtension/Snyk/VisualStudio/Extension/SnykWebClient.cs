namespace Snyk.VisualStudio.Extension
{
    using System.Net;

    /// <summary>
    /// Extended WebClient for Snyk extension.
    /// </summary>
    public class SnykWebClient : WebClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykWebClient"/> class.
        /// </summary>
        public SnykWebClient()
            : base()
        {
            this.Headers.Add("User-Agent", "SnykVisualStudioExtension");

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
    }
}
