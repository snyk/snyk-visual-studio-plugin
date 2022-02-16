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

        private static readonly ILogger Logger = LogManager.ForContext<ApiService>();

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
            try
            {
                var sastUrl = new Uri(this.GetSnykCodeSettingsUri(), SastSettingsApiName);

                using (var httpRequest = new HttpRequestMessage(HttpMethod.Get, sastUrl))
                {
                    httpRequest.Headers.Add("Authorization", $"token {this.options.ApiToken}");
                    httpRequest.Headers.Add("x-snyk-ide", $"vs-{SnykExtension.Version}");

                    var response = await this.httpClient.SendAsync(httpRequest);

                    string responseText = await response.Content.ReadAsStringAsync();

                    return Json.Deserialize<SastSettings>(responseText);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error on request sast settings.");

                return new SastSettings { SastEnabled = false };
            }
        }

        private Uri GetSnykCodeSettingsUri()
        {
            string customEndpoint = this.options.CustomEndpoint;

            if (string.IsNullOrEmpty(customEndpoint) || customEndpoint.Contains("https://snyk.io"))
            {
                return new Uri("https://snyk.io/api/");
            }

            if (customEndpoint.Contains("https://dev.snyk.io"))
            {
                return new Uri("https://dev.snyk.io/api/");
            }

            return !customEndpoint.EndsWith("/") ? new Uri(customEndpoint + "/") : new Uri(customEndpoint);
        }
    }
}
