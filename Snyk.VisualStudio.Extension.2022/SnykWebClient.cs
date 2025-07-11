using System.Net;
using System.Security.Cryptography.X509Certificates;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension
{
    /// <summary>
    /// Extended WebClient for Snyk extension.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")] // To prevent VS from changing this file subtype to "Component" in the .csproj
    public class SnykWebClient : WebClient
    {
        private readonly ISnykOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykWebClient"/> class.
        /// </summary>
        public SnykWebClient(ISnykOptions options = null)
            : base()
        {
            this.options = options;
            this.Headers.Add("User-Agent", "Snyk.VisualStudio.Extension");

            ServicePointManager.Expect100Continue = true;
            
            // Configure SSL/TLS settings - bypass certificate validation when IgnoreUnknownCA is enabled
            if (options?.IgnoreUnknownCA == true)
            {
                ServicePointManager.ServerCertificateValidationCallback = 
                    (sender, certificate, chain, sslPolicyErrors) => true;
            }
            
            // Note: Proxy configuration is not needed - WebClient uses system proxy by default
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && options?.IgnoreUnknownCA == true)
            {
                // Reset certificate validation callback to default
                ServicePointManager.ServerCertificateValidationCallback = null;
            }
            base.Dispose(disposing);
        }
    }
}
