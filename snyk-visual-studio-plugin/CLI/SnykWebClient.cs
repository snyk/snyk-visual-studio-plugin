using System.Net;

namespace Snyk.VisualStudio.Extension
{
    public class SnykWebClient : WebClient
    {        
        public SnykWebClient() : base()
        {
            this.Headers.Add("user-agent", "SnykVisualStudioExtension");

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
    }
}
