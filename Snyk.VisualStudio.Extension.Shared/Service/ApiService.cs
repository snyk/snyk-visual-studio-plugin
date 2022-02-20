namespace Snyk.VisualStudio.Extension.Shared.Service
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.Settings;

    /// <summary>
    /// Service for remote endpoint API work.
    /// </summary>
    public class ApiService : IApiService
    {
        private const string SastSettingsApiName = "cli-config/settings/sast";

        private ISnykOptions options;

        private HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiService"/> class.
        /// </summary>
        /// <param name="options">Options instance.</param>
        public ApiService(ISnykOptions options)
        {
            this.options = options;

            this.httpClient = HttpClientFactory.NewHttpClient(options.ApiToken);
        }

        /// <inheritdoc/>
        public async Task<SastSettings> GetSastSettingsAsync()
        {
            var settingsUrl = ApiEndpointResolver.NewInstance(this.options).GetSastUrl();
            var sastUrl = new Uri(new Uri(settingsUrl), SastSettingsApiName);

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Get, sastUrl))
            {
                httpRequest.Headers.Add("Authorization", $"token {this.options.ApiToken}");
                httpRequest.Headers.Add("x-snyk-ide", $"{SnykExtension.IntegrationName}-{SnykExtension.Version}");

                var response = await this.httpClient.SendAsync(httpRequest);

                string responseText = await response.Content.ReadAsStringAsync();

                return Json.Deserialize<SastSettings>(responseText);
            }
        }

        private Uri GetSnykCodeSettingsUri()
        {
            string customEndpoint = this.options.CustomEndpoint;
            string baseUrl;

            if (string.IsNullOrEmpty(customEndpoint))
            {
                baseUrl = "https://snyk.io/api/";
            }
            else
            {
                baseUrl = customEndpoint;
            }

            if (!baseUrl.EndsWith("/"))
            {
                baseUrl = baseUrl + "/";
            }

            return new Uri(baseUrl);
        }
    }
}
