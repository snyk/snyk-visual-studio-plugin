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

        private const string LoginApiUrl = "publicapi/login";

        private const string CheckSessionApiUrl = "publicapi/session";

        private const string FiltersApiUrl = "publicapi/filters";

        private const string BundleApiUrl = "publicapi/bundle";

        private const string FileApiUrl = "publicapi/file";

        private const string AnalysisApiUrl = "publicapi/analysis";

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

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Get, AnalysisApiUrl + "/" + bundleId))
            {
                var response = await this.httpClient.SendAsync(httpRequest, cancellationToken);

                string responseText = await response.Content.ReadAsStringAsync();

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

        /// <inheritdoc/>
        public async Task<bool> UploadFilesAsync(string bundleId, IEnumerable<CodeFileDto> codeFiles, CancellationToken cancellationToken = default)
        {
            Logger.Information("Upload files for bundle id {BundleId}.", bundleId);

            if (string.IsNullOrEmpty(bundleId))
            {
                throw new ArgumentException("Bundle id is null or empty.");
            }

            if (codeFiles == null)
            {
                throw new ArgumentException("Code files to upload is null.");
            }

            using (HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, FileApiUrl + "/" + bundleId))
            {
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                string payload = Json.Serialize(codeFiles);

                httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await this.httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                watch.Stop();

                Logger.Information("Execution Time: {ElapsedMilliseconds} ms", watch.ElapsedMilliseconds);

                return response.IsSuccessStatusCode;
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
                    var response = await this.httpClient.SendAsync(httpRequest);

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
        public async Task<BundleResponseDto> CheckBundleAsync(string bundleId, CancellationToken cancellationToken = default)
        {
            Logger.Information("Check bundle status with id {BundleId}.", bundleId);

            if (string.IsNullOrEmpty(bundleId))
            {
                throw new ArgumentException("Bundle id is null or empty.");
            }

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Get, BundleApiUrl + "/" + bundleId))
            {
                var response = await this.httpClient.SendAsync(httpRequest);

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
                string payload = Json.Serialize(new CreateBundleRequestDto
                {
                    Files = pathToHashFileDict,
                });

                using (httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json"))
                {
                    var response = await this.httpClient.SendAsync(httpRequest);

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

        /// <summary>
        /// Returns the list of allowed extensions and configuration files for uploaded bundles.
        /// </summary>
        /// <returns><see cref="FiltersDto"/></returns>
        public async Task<FiltersDto> GetFiltersAsync()
        {
            Logger.Information("Get SnykCode filters");

            using (var request = new HttpRequestMessage(HttpMethod.Get, FiltersApiUrl))
            {
                var response = await this.httpClient.SendAsync(request);

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
                var response = await this.httpClient.SendAsync(request);

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

        /// <summary>
        /// Check current session status with user token.
        /// </summary>
        /// <param name="sessionToken">User API token.</param>
        /// <returns><see cref="LoginStatus"/> object.</returns>
        public async Task<LoginStatus> CheckSessionAsync()
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, CheckSessionApiUrl))
            {
                HttpResponseMessage httpResponse = await this.httpClient.SendAsync(request);

                return new LoginStatus((int)httpResponse.StatusCode);
            }
        }
    }
}
