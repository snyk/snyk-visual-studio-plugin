namespace Snyk.VisualStudio.Extension.Service
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Settings;

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

            this.httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
            });

            this.httpClient.DefaultRequestHeaders.ExpectContinue = false;
            this.httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        }

        /// <summary>
        /// Request Sast settings by Settings custom endpoint and user token.
        /// </summary>
        /// <returns>Object of <see cref="SastSettings"/>.</returns>
        public async Task<SastSettings> GetSastSettingsAsync()
        {
            using (var httpRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(this.GetSnykCodeSettingsUri(), SastSettingsApiName)))
            {
                httpRequest.Headers.Accept.Clear();
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpRequest.Headers.Add("Authorization", $"token {this.options.ApiToken}");

                var response = await this.httpClient.SendAsync(httpRequest);

                string responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Json.Deserialize<SastSettings>(responseText);
                }
                else
                {
                    throw new Exception(responseText);
                }
            }
        }

        /// <summary>
        /// Request server and return SastEnabled or if some error occure return false.
        /// </summary>
        /// <returns>SnykCode enabled or disabled value.</returns>
        public async Task<bool> IsSnyCodeEnabledAsync()
        {
            bool isSnykCodeEnabled;

            try
            {
                var sastSettings = await this.GetSastSettingsAsync();

                isSnykCodeEnabled = sastSettings.SastEnabled;
            }
            catch (Exception e)
            {
                Logger.Error(e, "SnykApiService error.");

                isSnykCodeEnabled = false;
            }

            return isSnykCodeEnabled;
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
