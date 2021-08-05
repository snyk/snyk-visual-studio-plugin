namespace Snyk.Code.Library.Api
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Code.Library.Api.Dto;
    using Snyk.Code.Library.Api.Dto.Analysis;
    using Snyk.Common;

    /// <inheritdoc />
    public class SnykCodeClient: ISnykCodeClient
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
            ServicePointManager.Expect100Continue = false; // Fix issue with stream end on file upload.

            this.httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10),
                BaseAddress = new Uri(baseUrl),
            };

            this.httpClient.DefaultRequestHeaders.Add("Session-Token", token);

            Logger.Information("Create http client with with url {BaseUrl}.", baseUrl);
        }

        /// <summary>
        /// Starts a new bundle analysis or checks its current status and available results.
        /// Returns the current analysis status, the relative progress (between 0 and 1) within the current status, the analysisURL that you can access on your browser to see the interactive analysis on DeepCode, and the analysisResults if available. 
        /// The status is defined as follows:
        /// WAITING: Your request is waiting in a queue to be processed.
        /// FETCHING: The analysis has just begun and it is currently cloning/fetching the git repository or checking missing files.
        /// ANALYZING: DeepCode is analyzing every file in the bundle to check for bugs and create suggestions.
        /// DC_DONE: DeepCode has finished analyzing the files but external linter tools are still computing.
        /// DONE: All analyses have been computed and are available.
        /// FAILED: Something went wrong with the analysis. For uploaded bundles this occurs when attempting to analyze bundles with missing files.If caused by a transient error, further calls to this API will reset the analysis status and start from the "FETCHING" phase again.
        /// The analysisResults object is only available in the "DONE" status.
        /// It contains all the suggestions and the relative positions.
        /// </summary>
        /// <param name="bundleId">Source bundle id to analysy.</param>
        /// <returns>Analysis results with suggestions and the relative positions.</returns>
        public async Task<AnalysisResultDto> GetAnalysisAsync(string bundleId)
        {
            Logger.Information("Get analysis result for bundle id {BundleId}.", bundleId);

            if (string.IsNullOrEmpty(bundleId))
            {
                throw new ArgumentException("Bundle id is null or empty.");
            }

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Get, AnalysisApiUrl + "/" + bundleId))
            {
                var response = await this.httpClient.SendAsync(httpRequest);

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

        /// <summary>
        /// Uploads missing files to a bundle.
        /// Small files should be uploaded in batches to avoid excessive overhead due to too many requests. 
        /// The file contents must be utf-8 parsed strings and the file hashes must be computed over these strings, matching the "Create Bundle" request.
        /// </summary>
        /// <param name="bundleId">Bundle id to file upload.</param>
        /// <param name="codeFiles">List of <see cref="CodeFileDto"/> with file hash and file content.</param>
        /// <returns>True if upload success.</returns>
        public async Task<bool> UploadFilesAsync(string bundleId, IEnumerable<CodeFileDto> codeFiles)
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

                var response = await this.httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

                watch.Stop();

                Logger.Information("Execution Time: {ElapsedMilliseconds} ms", watch.ElapsedMilliseconds);

                return response.IsSuccessStatusCode;
            }
        }

        /// <summary>
        /// Creates a new bundle based on a previously uploaded one.
        /// The newly created child bundle will have the same files as the parent bundle (identified by the bundleId in the request) except for what is defined in the payload. 
        /// The removedFiles are parsed before the files, therefore if the same filePath appears in both of them it will not be removed. 
        /// The entries in the files object can either replace an old file with a new version (if the paths match) or add a new file to the child bundle. 
        /// This API is only available for extending uploaded bundles (not git bundles).
        /// As per the "Create Bundle" API, it is possible to pass either an object or an array in the file parameter, with the same semantics as previously described.
        /// Extending a bundle by removing all the parent bundle's files is not allowed.
        /// </summary>
        /// <param name="bundleId">Already created bundle id.</param>
        /// <param name="pathToHashFileDict">Files to add in bundle.</param>
        /// <param name="removedFiles">Files to remove in bundle.</param>
        /// <returns>Extended bundle object.</returns>
        public async Task<BundleResponseDto> ExtendBundleAsync(string bundleId, Dictionary<string, string> pathToHashFileDict, List<string> removedFiles)
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

        /// <summary>
        /// Checks the status of a bundle.
        /// </summary>
        /// <param name="bundleId">Bundle id to check.</param>
        /// <returns
        /// >Returns the bundleId and, in case of uploaded bundles, the current missingFiles and the uploadURL.
        /// This API can be used to check if an old uploaded bundle has expired (status code 404),
        /// or to check if there are still missing files after uploading ("Upload Files").
        /// </returns>
        public async Task<BundleResponseDto> CheckBundleAsync(string bundleId)
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

        /// <summary>
        /// Create new <see cref="BundleResponseDto"/> and get result <see cref="BundleResponseDto"/> object.
        /// </summary>
        /// <param name="pathToHashFileDict">Bundle files.</param>
        /// <returns>Bundle object with bundle id, missing files and upload url.</returns>
        public async Task<BundleResponseDto> CreateBundleAsync(IDictionary<string, string> pathToHashFileDict)
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
