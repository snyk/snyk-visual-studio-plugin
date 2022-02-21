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

            this.httpClient = HttpClientFactory.NewHttpClient(options.ApiToken);
        }

        /// <inheritdoc/>
        public async Task<SastSettings> GetSastSettingsAsync()
        {
            var settingsUrl = new ApiEndpointResolver(this.options).GetSnykApiEndpoint();
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
    }
}
