namespace Snyk.Common.Service
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Snyk.Common.Settings;

    /// <summary>
    /// Service for remote endpoint API work.
    /// </summary>
    public class SnykApiService : ISnykApiService
    {
        private const string SastSettingsApiName = "v1/cli-config/settings/sast";

        private readonly ISnykOptions options;
        private readonly string vsVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykApiService"/> class.
        /// </summary>
        /// <param name="options">Options instance.</param>
        /// <param name="vsVersion">The IDE major version (17 for vs22)</param>
        /// <param name="pluginVersion">The full plugin version</param>
        public SnykApiService(ISnykOptions options, string vsVersion = "", string pluginVersion = "")
        {
            this.options = options;
            this.vsVersion = vsVersion ?? "";
        }

        private HttpClient HttpClient => HttpClientFactory.NewHttpClient(this.options.ApiToken)
            .WithUserAgent(this.vsVersion, SnykExtension.Version);

        /// <inheritdoc/>
        public async Task<SnykUser> GetUserAsync()
        {
            var endpoint = new ApiEndpointResolver(this.options).UserMeEndpoint;

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Get, endpoint))
            {
                var response = await HttpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                return Json.Deserialize<SnykUser>(responseContent);
            }
        }

        /// <inheritdoc/>
        public async Task<SastSettings> GetSastSettingsAsync()
        {
            if (!this.options.ApiToken.IsValid())
            {
                return null;
            }

            var response = await SendSastSettingsRequestAsync();
            var responseContent = await response.Content.ReadAsStringAsync();

            try
            {
                return Json.Deserialize<SastSettings>(responseContent);
            }
            catch (Exception e)
            {
                // In case of invalid json string return null.
                return null;
            }
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
                httpRequest.Headers.Add("x-snyk-ide", $"{SnykExtension.IntegrationName}-{SnykExtension.Version}");

                return await HttpClient.SendAsync(httpRequest);
            }
        }
    }
}