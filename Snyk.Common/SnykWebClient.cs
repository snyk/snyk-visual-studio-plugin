namespace Snyk.Common
{
    using System.Net;

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
        }
    }
}
