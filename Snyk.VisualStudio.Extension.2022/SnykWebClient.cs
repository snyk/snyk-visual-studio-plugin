using System.Net;

namespace Snyk.VisualStudio.Extension
{
    /// <summary>
    /// Extended WebClient for Snyk extension.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")] // To prevent VS from changing this file subtype to "Component" in the .csproj
    public class SnykWebClient : WebClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykWebClient"/> class.
        /// </summary>
        public SnykWebClient()
            : base()
        {
            this.Headers.Add("User-Agent", "Snyk.VisualStudio.Extension");

            ServicePointManager.Expect100Continue = true;

            // WebClient defaults to 100 seconds. Every request made through this client is a small
            // metadata fetch (the release version file and its .sha256), and some of them are on the
            // startup path, so a blackholed mirror should fail fast rather than stall the IDE.
            this.Timeout = 15000;
        }

        /// <summary>
        /// Request timeout in milliseconds. WebClient does not expose one, so it is applied to each
        /// underlying request below.
        /// </summary>
        public int Timeout { get; set; }

        protected override WebRequest GetWebRequest(System.Uri address)
        {
            var request = base.GetWebRequest(address);

            if (request != null && this.Timeout > 0)
            {
                request.Timeout = this.Timeout;
                request.ReadWriteTimeout = this.Timeout;
            }

            return request;
        }
    }
}
