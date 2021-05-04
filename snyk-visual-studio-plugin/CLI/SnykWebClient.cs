using System.Net;

namespace Snyk.VisualStudio.Extension
{
    public class SnykWebClient : WebClient
    {        
        public SnykWebClient() : base()
        {
            this.Headers.Add("User-Agent", "snyk-visual-studio-plugin");

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
    }
}
