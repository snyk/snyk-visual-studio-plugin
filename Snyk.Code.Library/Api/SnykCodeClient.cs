namespace Snyk.Code.Library.Api
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Code.Library.Api.Dto;
    using Snyk.Code.Library.Api.Dto.Analysis;
    using Snyk.Code.Library.Api.Encoding;
    using Snyk.Common;
    using Snyk.Common.Authentication;

    /// <inheritdoc />
    public class SnykCodeClient : ISnykCodeClient
    {
        /// <summary>
        /// Maximum bundle size per one upload is 4 Mb. 4 Mb in bytes.
        /// </summary>
        public const int MaxBundleSize = 4_000_000;

        private const string FiltersApiUrl = "filters";

        private const string BundleApiUrl = "bundle";

        private const string AnalysisApiUrl = "analysis";

        private static readonly ILogger Logger = LogManager.ForContext<SnykCodeClient>();

        private readonly HttpClient httpClient;

        private string contextFlowName;

        private string contextOrgName;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeClient"/> class.
        /// </summary>
        /// <param name="baseUrl">Base URL for deproxy.</param>
        /// <param name="token">User token.</param>
        /// <param name="flowName">Context flow name.</param>
        /// <param name="orgName">User organization name.</param>
        public SnykCodeClient(string baseUrl, AuthenticationToken token, string flowName, string orgName)
        {
            this.httpClient = HttpClientFactory.NewHttpClient(token, baseUrl);

            Logger.Information("Create http client with with url {BaseUrl}.", baseUrl);

            this.contextFlowName = flowName;
            this.contextOrgName = orgName;
        }

        /// <inheritdoc/>
        public async Task<AnalysisResultDto> GetAnalysisAsync(string bundleId, CancellationToken cancellationToken = default)
        {
            Logger.Information("Get analysis result for bundle id {BundleId}.", bundleId);

            if (string.IsNullOrEmpty(bundleId))
            {
                throw new ArgumentException("Bundle id is null or empty.");
            }

            string payload = this.GetAnalysisResultRequestPayload(bundleId);

            using (var requestContent = await this.NewHttpRequestContentAsync(HttpMethod.Post, payload))
            using (var httpRequest = this.NewHttpRequestMessage(HttpMethod.Post, AnalysisApiUrl, requestContent))
            using (var response = await this.httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                using (var content = response.Content)
                {
                    string responseText = await content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        return Json.Deserialize<AnalysisResultDto>(responseText);
                    }

                    throw new SnykCodeException((int)response.StatusCode, responseText);
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

            string payload = Json.Serialize(new UploadFilesExtendBundleRequestDto
            {
                Files = hashToContentDict,
            });

            using (var content = await this.NewHttpRequestContentAsync(HttpMethod.Put, payload))
            using (var httpRequest = this.NewHttpRequestMessage(HttpMethod.Put, BundleApiUrl + "/" + bundleId, content))
            using (var response = await this.httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Json.Deserialize<BundleResponseDto>(responseText);
                }

                const int maxResponseLengthInLog = 1000;
                var loggedResponse = responseText;
                if (responseText == null)
                {
                    loggedResponse = "Empty response";
                }
                else if(responseText.Length > maxResponseLengthInLog)
                {
                    loggedResponse = responseText.Substring(0, maxResponseLengthInLog);
                }
                Logger.Error("Error during bundle extension. Status code: {StatusCode}. Response: {Response}", response.StatusCode, loggedResponse);
                throw new SnykCodeException((int)response.StatusCode, responseText);
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

            string payload = Json.Serialize(new ExtendBundleRequestDto
            {
                Files = pathToHashFileDict,
                RemovedFiles = removedFiles,
            });

            using (var content = await this.NewHttpRequestContentAsync(HttpMethod.Put, payload))
            using (var httpRequest = this.NewHttpRequestMessage(HttpMethod.Put, BundleApiUrl + "/" + bundleId, content))
            using (var response = await this.httpClient.SendAsync(httpRequest, cancellationToken))
            {
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Json.Deserialize<BundleResponseDto>(responseText);
                }

                throw new SnykCodeException((int)response.StatusCode, responseText);
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

            using (var httpRequest = this.NewHttpRequestMessage(HttpMethod.Get, BundleApiUrl + "/" + bundleId, null))
            using (var response = await this.httpClient.SendAsync(httpRequest, cancellationToken))
            {
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Json.Deserialize<BundleResponseDto>(responseText);
                }

                throw new SnykCodeException((int)response.StatusCode, responseText);
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

            string payload = Json.Serialize(pathToHashFileDict);

            using (var content = await this.NewHttpRequestContentAsync(HttpMethod.Post, payload))
            using (var httpRequest = this.NewHttpRequestMessage(HttpMethod.Post, BundleApiUrl, content))
            using (var response = await this.httpClient.SendAsync(httpRequest, cancellationToken))
            {
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Json.Deserialize<BundleResponseDto>(responseText);
                }

                throw new SnykCodeException((int)response.StatusCode, responseText);
            }
        }

        /// <summary>
        /// Returns the list of allowed extensions and configuration files for uploaded bundles.
        /// </summary>
        /// <returns><see cref="FiltersDto"/></returns>
        public async Task<FiltersDto> GetFiltersAsync()
        {
            Logger.Information("Get SnykCode filters");

            using (var request = this.NewHttpRequestMessage(HttpMethod.Get, FiltersApiUrl, null))
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

        /// <inheritdoc/>
        public string GetAnalysisResultRequestPayload(string bundleId)
            => Json.Serialize(new AnalysisResultRequestDto
            {
                Key = new AnalysisResultKeyDto
                {
                    Type = "file",
                    Hash = bundleId,
                },
                AnalysisContext = new AnalysisContextDto
                {
                    Flow = this.contextFlowName,
                    OrgDisplayName = this.contextOrgName,
                },
                Legacy = true,
            });

        private HttpRequestMessage NewHttpRequestMessage(HttpMethod method, string requestUri, HttpContent content)
        {
            var request = new HttpRequestMessage(method, requestUri);
            request.Content = content;

            if (method == HttpMethod.Put || method == HttpMethod.Post)
            {
                request.Content.Headers.Add("Content-Type", "application/octet-stream");
                request.Content.Headers.Add("Content-Encoding", "gzip");
            }

            return request;
        }

        private async Task<HttpContent> NewHttpRequestContentAsync(HttpMethod method, string payload)
        {
            // Snyk Code PUT and POST requests must be base64 encoded and deflated for certain environments (ROAD-909)
            if (method == HttpMethod.Put || method == HttpMethod.Post)
            {
                Logger.Information("Encoding and compressing {Length} bytes...", System.Text.Encoding.UTF8.GetByteCount(payload));
                var encodedPayload = await Encoder.EncodeAndCompressAsync(payload);
                var byteContent = encodedPayload.ToArray();
                Logger.Information("Sending {Length} bytes", byteContent.Length);

                return new ByteArrayContent(byteContent);
            }

            Logger.Information("Sending {Length} bytes...", System.Text.Encoding.UTF8.GetByteCount(payload));
            return new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        }
    }
}
