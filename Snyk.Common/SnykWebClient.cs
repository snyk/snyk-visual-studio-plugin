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
            
            // TODO: Get back to this and find an alternative
#pragma warning disable RS0030 // Do not used banned APIs
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
#pragma warning restore RS0030 // Do not used banned APIs
        }
    }
}
