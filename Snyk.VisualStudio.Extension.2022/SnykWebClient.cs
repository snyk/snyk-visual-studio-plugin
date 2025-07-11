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
            
            // Configure SSL/TLS settings
            if (options?.IgnoreUnknownCA == true)
            {
                ServicePointManager.ServerCertificateValidationCallback = 
                    (sender, certificate, chain, sslPolicyErrors) => true;
            }
            
            // Configure proxy settings if available
            ConfigureProxy();
        }

        private void ConfigureProxy()
        {
            try
            {
                // Use system proxy by default
                this.Proxy = WebRequest.GetSystemWebProxy();
                
                // If proxy is configured, use credentials
                if (this.Proxy != null)
                {
                    this.Proxy.Credentials = CredentialCache.DefaultCredentials;
                }
            }
            catch (System.Exception)
            {
                // If proxy configuration fails, continue without proxy
                this.Proxy = null;
            }
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
