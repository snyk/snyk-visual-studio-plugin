namespace Snyk.VisualStudio.Extension.Settings
{
    public class SnykGeneralOptions
    {
        private string apiToken = "";
        private string customEndpoint = "";
        private string organization = "";
        private bool ignoreUnknownCA = false;

        public string ApiToken
        {
            get { return apiToken; }
            set { apiToken = value; }
        }

        public string CustomEndpoint
        {
            get { return customEndpoint; }
            set { customEndpoint = value; }
        }

        public string Organization
        {
            get { return organization; }
            set { organization = value; }
        }

        public bool IgnoreUnknownCA
        {
            get { return ignoreUnknownCA; }
            set { ignoreUnknownCA = value; }
        }
    }
}
