namespace Snyk.Code.Library.Api
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Code.Library.Api.Dto;
    using Snyk.Code.Library.Api.Dto.Analysis;
    using Snyk.Common;

    /// <inheritdoc />
    public class SnykCodeClient : ISnykCodeClient
    {
        /// <summary>
        /// Maxium bundle size per one upload is 4 Mb. 4 Mb in bytes.
        /// </summary>
        public const int MaxBundleSize = 4000000;

        private const string LoginApiUrl = "login";

        private const string FiltersApiUrl = "filters";

        private const string BundleApiUrl = "bundle";

        private const string AnalysisApiUrl = "analysis";

        private static readonly ILogger Logger = LogManager.ForContext<SnykCodeClient>();

        private readonly HttpClient httpClient;

        private LoginResponseDto loginResponse;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeClient"/> class.
        /// </summary>
        /// <param name="baseUrl">Base URL for deproxy.</param>
        /// <param name="token">User token.</param>
        public SnykCodeClient(string baseUrl, string token)
        {
            this.httpClient = HttpClientFactory.NewHttpClient(token, baseUrl);

            Logger.Information("Create http client with with url {BaseUrl}.", baseUrl);
        }

        /// <inheritdoc/>
        public async Task<AnalysisResultDto> GetAnalysisAsync(string bundleId, CancellationToken cancellationToken = default)
        {
            Logger.Information("Get analysis result for bundle id {BundleId}.", bundleId);

            if (string.IsNullOrEmpty(bundleId))
            {
                throw new ArgumentException("Bundle id is null or empty.");
            }

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, AnalysisApiUrl))
            {
                string payload = Json.Serialize(new AnalysisResultRequestDto
                {
                    Key = new AnalysisResultKeyDto
                    {
                        Type = "file",
                        Hash = bundleId,
                    },
                    Legacy = true,
                });

                httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                using (var response = await this.httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    using (var content = response.Content)
                    {
                        string responseText = await content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            return Json.Deserialize<AnalysisResultDto>(responseText);
                        }
                        else
                        {
                            throw new SnykCodeException((int)response.StatusCode, responseText);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<BundleResponseDto> ExtendBundleAsync(
            string bundleId,
            Dictionary<string, CodeFileDto> hashToContentDict,
            CancellationToken cancellationToken = default)
        {
            Logger.Information("Extend bundle for bundle id {BundleId}.", bundleId);

            if (string.IsNullOrEmpty(bundleId))
            {
                throw new ArgumentException("Previous Bundle is null or empty.");
            }

            if (hashToContentDict == null)
            {
                throw new ArgumentException("Files or removed files are null.");
            }

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Put, BundleApiUrl + "/" + bundleId))
            {
                string payload = Json.Serialize(new UploadFilesExtendBundleRequestDto
                {
                    Files = hashToContentDict,
                });

                using (httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json"))
                {
                    var response = await this.httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                    string responseText = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        return Json.Deserialize<BundleResponseDto>(responseText);
                    }
                    else
                    {
                        throw new SnykCodeException((int)response.StatusCode, responseText);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<BundleResponseDto> ExtendBundleAsync(
            string bundleId,
            IDictionary<string, string> pathToHashFileDict,
            IEnumerable<string> removedFiles,
            CancellationToken cancellationToken = default)
        {
            Logger.Information("Extend bundle for bundle id {BundleId}.", bundleId);

            if (string.IsNullOrEmpty(bundleId))
            {
                throw new ArgumentException("Previous Bundle is null or empty.");
            }

            if (pathToHashFileDict == null || removedFiles == null)
            {
                throw new ArgumentException("Files or removed files are null.");
            }

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Put, BundleApiUrl + "/" + bundleId))
            {
                string payload = Json.Serialize(new ExtendBundleRequestDto
                {
                    Files = pathToHashFileDict,
                    RemovedFiles = removedFiles,
                });

                using (httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json"))
                {
                    using (var response = await this.httpClient.SendAsync(httpRequest))
                    {
                        string responseText = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            return Json.Deserialize<BundleResponseDto>(responseText);
                        }
                        else
                        {
                            throw new SnykCodeException((int)response.StatusCode, responseText);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<BundleResponseDto> CheckBundleAsync(string bundleId, CancellationToken cancellationToken = default)
        {
            Logger.Information("Check bundle status with id {BundleId}.", bundleId);

            if (string.IsNullOrEmpty(bundleId))
            {
                throw new ArgumentException("Bundle id is null or empty.");
            }

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Get, BundleApiUrl + "/" + bundleId))
            {
                using (var response = await this.httpClient.SendAsync(httpRequest))
                {
                    string responseText = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        return Json.Deserialize<BundleResponseDto>(responseText);
                    }
                    else
                    {
                        throw new SnykCodeException((int)response.StatusCode, responseText);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<BundleResponseDto> CreateBundleAsync(IDictionary<string, string> pathToHashFileDict, CancellationToken cancellationToken = default)
        {
            Logger.Information("Create bundle files count {Count}", pathToHashFileDict.Count);

            if (pathToHashFileDict == null)
            {
                throw new ArgumentException("Bundle files is null.");
            }

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, BundleApiUrl))
            {
                string payload = Json.Serialize(pathToHashFileDict);

                using (httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json"))
                {
                    using (var response = await this.httpClient.SendAsync(httpRequest))
                    {
                        string responseText = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            return Json.Deserialize<BundleResponseDto>(responseText);
                        }
                        else
                        {
                            throw new SnykCodeException((int)response.StatusCode, responseText);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the list of allowed extensions and configuration files for uploaded bundles.
        /// </summary>
        /// <returns><see cref="FiltersDto"/></returns>
        public async Task<FiltersDto> GetFiltersAsync()
        {
            Logger.Information("Get SnykCode filters");

            using (var request = new HttpRequestMessage(HttpMethod.Get, FiltersApiUrl))
            {
                using (var response = await this.httpClient.SendAsync(request))
                {
                    string responseText = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        return Json.Deserialize<FiltersDto>(responseText);
                    }
                    else
                    {
                        throw new SnykCodeException((int)response.StatusCode, responseText);
                    }
                }
            }
        }

        /// <summary>
        /// Requests the creation of a new login session.
        /// <param name="userAgent">Represents requested client. For example, VisualStudio or VisualStudio code or other IDE.</param>
        /// /// <returns><see cref="LoginResponseDto"/> object.</returns>
        /// </summary>
        /// <param name="userAgent">Optional parameter with again (VisualStudio for example).</param>
        /// <returns><see cref="loginResponse"/> object.</returns>
        public async Task<LoginResponseDto> LoginAsync(string userAgent = "")
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                throw new ArgumentException("User agent is null or empty");
            }

            using (var request = new HttpRequestMessage(HttpMethod.Post, LoginApiUrl))
            {
                using (var response = await this.httpClient.SendAsync(request))
                {
                    string responseText = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        this.loginResponse = Json.Deserialize<LoginResponseDto>(responseText);

                        return this.loginResponse;
                    }
                    else
                    {
                        throw new SnykCodeException((int)response.StatusCode, responseText);
                    }
                }
            }
        }
    }
}
