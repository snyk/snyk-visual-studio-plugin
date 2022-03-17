namespace Snyk.VisualStudio.Extension.Shared.Service
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.Settings;

    /// <summary>
    /// Service for remote endpoint API work.
    /// </summary>
    public class SnykApiService : ISnykApiService
    {
        private const string SastSettingsApiName = "cli-config/settings/sast";

        private ISnykOptions options;

        private HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykApiService"/> class.
        /// </summary>
        /// <param name="options">Options instance.</param>
        public SnykApiService(ISnykOptions options)
        {
            this.options = options;

            this.httpClient = HttpClientFactory.NewHttpClient(this.options.ApiToken);
        }

        /// <inheritdoc/>
        public async Task<SastSettings> GetSastSettingsAsync()
        {
            if (!Common.Guid.IsValid(this.options.ApiToken))
            {
                return null;
            }

            var response = await this.SendSastSettingsRequestAsync();
            var responseContent = await response.Content.ReadAsStringAsync();

            return Json.Deserialize<SastSettings>(responseContent);
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> SendSastSettingsRequestAsync()
        {
            var settingsUrl = new ApiEndpointResolver(this.options).GetSnykApiEndpoint();
            var builder = new UriBuilder(new Uri(new Uri(settingsUrl), SastSettingsApiName).ToString());
            var organization = this.options.Organization;
            if (!string.IsNullOrEmpty(organization))
            {
                builder.Query = $"org={organization}";
            }

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Get, builder.Uri))
            {
                httpRequest.Headers.Add("Authorization", $"token {this.options.ApiToken}");
                httpRequest.Headers.Add("x-snyk-ide", $"{SnykExtension.IntegrationName}-{SnykExtension.Version}");

                return await this.httpClient.SendAsync(httpRequest);
            }
        }
    }
}
