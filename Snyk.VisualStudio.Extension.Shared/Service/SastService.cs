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
    public class SastService : ISastService
    {
        private const string SastSettingsApiName = "cli-config/settings/sast";

        private static readonly ILogger Logger = LogManager.ForContext<SastService>();

        private ISnykOptions options;

        private HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="SastService"/> class.
        /// </summary>
        /// <param name="options">Options instance.</param>
        public SastService(ISnykOptions options)
        {
            this.options = options;

            this.httpClient = HttpClientFactory.NewHttpClient(options.ApiToken);
        }

        /// <inheritdoc/>
        public async Task<SastSettings> GetSastSettingsAsync()
        {
            try
            {
                using (var httpRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(this.GetSnykCodeSettingsUri(), SastSettingsApiName)))
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
