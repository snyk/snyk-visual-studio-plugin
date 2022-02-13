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
    public class SnykApiService
    {
        private const string SastSettingsApiName = "cli-config/settings/sast";

        private static readonly ILogger Logger = LogManager.ForContext<SnykApiService>();

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

        /// <summary>
        /// Request Sast settings by Settings custom endpoint and user token.
        /// </summary>
        /// <returns>Object of <see cref="SastSettings"/>.</returns>
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

                    if (response.IsSuccessStatusCode)
                    {
                        return Json.Deserialize<SastSettings>(responseText);
                    }
                    else
                    {
                        return new SastSettings { SastEnabled = false };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error on request sast settings.");

                return new SastSettings { SastEnabled = false };
            }
        }

        /// <summary>
        /// Request server and return SastEnabled or if some error occure return false.
        /// </summary>
        /// <returns>SnykCode enabled or disabled value.</returns>
        public async Task<bool> IsSnykCodeEnabledAsync() => (await this.GetSastSettingsAsync()).SastEnabled;

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
