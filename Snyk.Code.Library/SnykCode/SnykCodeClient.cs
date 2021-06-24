namespace Snyk.Code.Library.SnykCode
{    
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Snyk.Code.Library.Common;

    /// <summary>
    /// Client for SnykCode support.
    /// </summary>
    public class SnykCodeClient
    {
        private const string LoginApiUrl = "publicapi/login";

        private const string CheckSessionApiUrl = "publicapi/session";

        private const string FiltersApiUrl = "publicapi/filters";

        private const string BundleApiUrl = "publicapi/bundle";

        private const string FileApiUrl = "publicapi/file";

        /// <summary>
        /// Maxium bundle size per one upload is 4 Mb. 4 Mb in bytes.
        /// </summary>
        private const int MaxBundleSize = 4000000;

        private readonly HttpClient httpClient = new HttpClient();

        private LoginResponse loginResponse;
        
        private string snykCodeBaseUrl;

        private string apiToken;        

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeClient"/> class.
        /// </summary>
        /// <param name="baseUrl">Base URL for deproxy.</param>
        /// <param name="token">User token.</param>
        public SnykCodeClient(string baseUrl, string token)
        {
            this.snykCodeBaseUrl = baseUrl;
            this.apiToken = token;

            this.httpClient.Timeout = TimeSpan.FromMinutes(10);
        }

        public async System.Threading.Tasks.Task<Bundle> UploadFiles(Bundle bundle, List<CodeFile> codeFiles)
        {
            if (bundle == null || string.IsNullOrEmpty(bundle.Id))
            {
                throw new ArgumentException("Bundle is null or empty.");
            }

            if (codeFiles == null)
            {
                throw new ArgumentException("Code files to upload is null.");
            }

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post,
                this.snykCodeBaseUrl + FileApiUrl + "/" + bundle.Id);

            httpRequest.Version = HttpVersion.Version10;
            httpRequest.Headers.Add("Session-Token", apiToken);

            string payload = Json.Serialize<List<CodeFile>>(codeFiles);
            httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(httpRequest);

            string responseTest = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Json.Deserialize<Bundle>(responseTest);
            }
            else
            {
                throw new SnykCodeException(((int)response.StatusCode), responseTest);
            }
        }

        /// <summary>
        /// Creates a new bundle based on a previously uploaded one.
        /// This method wrap functionality to extend bundle if it's small by size or make few chunks and extend by chunks.
        /// </summary>
        /// <param name="previousBundle">Already created bundle with valid bundle id.</param>
        /// <param name="extendBundle">Bundle to extend with new or removed files.</param>
        /// <param name="maxBundleChunkSize">Maximum bundle chunk size. By default it is 4 Mb.</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<Bundle> ExtendBundle(Bundle previousBundle, Bundle extendBundle, int maxBundleChunkSize = MaxBundleSize)
        {
            if (previousBundle == null || string.IsNullOrEmpty(previousBundle.Id))
            {
                throw new ArgumentException("Previous Bundle is null or empty.");
            }

            if (extendBundle == null)
            {
                throw new ArgumentException("Extend Bundle is null or empty.");
            }

            int payloadSize = calculateBundleSize(extendBundle);

            // If payload < 4 max bundle chunk size just send this bundle and return results.
            if (payloadSize < maxBundleChunkSize)
            {
                return await this.ExtendOneChunkBundle(previousBundle, extendBundle, maxBundleChunkSize);
            }
            else
            {
                return await this.ExtendMultiChunkBundle(previousBundle, extendBundle, maxBundleChunkSize);
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
        /// <param name="previousBundle">Already created bundle with valid bundle id.</param>
        /// <param name="extendBundle">Bundle to extend with new or removed files.</param>
        /// <param name="maxBundleChunkSize">Maximum bundle chunk size. By default it is 4 Mb.</param>
        /// <returns>Extended bundle object.</returns>
        public async System.Threading.Tasks.Task<Bundle> ExtendOneChunkBundle(Bundle previousBundle, Bundle extendBundle, int maxBundleChunkSize = MaxBundleSize)
        {
            if (previousBundle == null || string.IsNullOrEmpty(previousBundle.Id))
            {
                throw new ArgumentException("Previous Bundle is null or empty.");
            }

            if (extendBundle == null)
            {
                throw new ArgumentException("Extend Bundle is null or empty.");
            }

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Put,
                this.snykCodeBaseUrl + BundleApiUrl + "/" + previousBundle.Id);

            httpRequest.Version = HttpVersion.Version10;
            httpRequest.Headers.Add("Session-Token", apiToken);

            httpRequest.Content = new StringContent(Json.Serialize<Bundle>(extendBundle), Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(httpRequest);

            string responseTest = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Json.Deserialize<Bundle>(responseTest);
            }
            else
            {
                throw new SnykCodeException(((int)response.StatusCode), responseTest);
            }
        }

        /// <summary>
        /// Checks the status of a bundle.
        /// </summary>
        /// <param name="uploadedBundle"></param>
        /// <returns>Returns the bundleId and, in case of uploaded bundles, the current missingFiles and the uploadURL. 
        /// This API can be used to check if an old uploaded bundle has expired (status code 404), 
        /// or to check if there are still missing files after uploading ("Upload Files").</returns>
        public async System.Threading.Tasks.Task<Bundle> CheckBundle(Bundle uploadedBundle)
        {
            if (uploadedBundle == null || string.IsNullOrEmpty(uploadedBundle.Id))
            {
                throw new ArgumentException("Bundle is null or empty.");
            }

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, this.snykCodeBaseUrl + BundleApiUrl + "/" + uploadedBundle.Id);

            httpRequest.Headers.Add("Session-Token", apiToken);

            var response = await httpClient.SendAsync(httpRequest);

            string responseTest = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Json.Deserialize<Bundle>(responseTest);
            }
            else
            {
                throw new SnykCodeException(((int)response.StatusCode), responseTest);
            }
        }

        /// <summary>
        /// Create new <see cref="Bundle"/> and get result <see cref="Bundle"/> object.
        // If payload < 4 Mb it just send this bundle and return results.
        // If payload > 4 Mb it will:
        //      Split initial bundle on list of bundles (chunks).
        //      Call Create bundle REST API for first bundle in list.
        //      For all other bundles it will Extend bundle.
        //      Return last bundle as result.
        /// </summary>
        /// <param name="newBundle">Bundle object with files data.</param>
        /// <returns>Bundle object with bundle id, missing files and upload url.</returns>
        public async System.Threading.Tasks.Task<Bundle> CreateBundle(Bundle newBundle, int maxBundleChunkSize = MaxBundleSize)
        {
            if (newBundle == null || newBundle.Files.Count == 0)
            {
                throw new ArgumentException("Bundle is null or empty.");
            }

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, this.snykCodeBaseUrl + BundleApiUrl);

            httpRequest.Headers.Add("Session-Token", apiToken);

            int payloadSize = calculateBundleSize(newBundle);

            // If payload < 4 Mb just send this bundle and return results.
            if (payloadSize < maxBundleChunkSize)
            {
                return await this.CreateOneChunkBundle(newBundle);
            }
            else
            {
                return await this.CreateMultiChunkBundle(newBundle);
            }
        }

        /// <summary>
        /// Split big bundle to list of small bundles and create new bundle on server using this "chunk" bundles.
        /// </summary>
        /// <param name="newBundle">Source bundle.</param>
        /// <param name="maxBundleChunkSize">Maximum bundle size. By default it's 4 Mb.</param>
        /// <returns>Result Bundle object from server.</returns>
        public async System.Threading.Tasks.Task<Bundle> CreateMultiChunkBundle(Bundle newBundle, int maxBundleChunkSize = MaxBundleSize)
        {
            List<Bundle> bundles = SplitBundleToChunksBySize(newBundle, maxBundleChunkSize);

            Bundle firstBundle = bundles[0];

            // Call Create Bundle REST API for first bundle in list to create it on server.
            Bundle resultBundle = await this.CreateOneChunkBundle(firstBundle);

            bundles.Remove(firstBundle);

            // Call Extend Bundle REST API for bundles.
            foreach (Bundle bundleItem in bundles)
            {
                resultBundle = await this.ExtendOneChunkBundle(resultBundle, bundleItem);
            }

            // Last created bundle is result bundle.
            return resultBundle;
        }

        /// <summary>
        /// Split big bundle to list of small bundles and extend bundle using this "chunk" bundles.
        /// </summary>
        /// <param name="newBundle">Source bundle.</param>
        /// <param name="maxBundleChunkSize">Maximum bundle size. By default it's 4 Mb.</param>
        /// <returns>Result Bundle object from server.</returns>
        public async System.Threading.Tasks.Task<Bundle> ExtendMultiChunkBundle(Bundle previousBundle, Bundle extendBundle, int maxBundleChunkSize = MaxBundleSize)
        {
            List<Bundle> bundles = SplitBundleToChunksBySize(extendBundle, maxBundleChunkSize);

            Bundle firstBundle = bundles[0];

            // Call Create Bundle REST API for first bundle in list to create it on server.
            Bundle resultBundle = await this.ExtendOneChunkBundle(previousBundle, firstBundle, maxBundleChunkSize);

            bundles.Remove(firstBundle);

            // Call Extend Bundle REST API for bundles.
            foreach (Bundle bundleItem in bundles)
            {
                resultBundle = await this.ExtendOneChunkBundle(resultBundle, bundleItem);
            }

            // Last created bundle is result bundle.
            return resultBundle;
        }

        /// <summary>
        /// Split bundle to list of bundles by maximun bundle size.
        /// </summary>
        /// <param name="newBundle">Source bundle.</param>
        /// <param name="maxBundleChunkSize">Maximum bundle size. By default it's 4 Mb.</param>
        /// <returns>List<Bundle>.</returns>
        public List<Bundle> SplitBundleToChunksBySize(Bundle newBundle, int maxBundleChunkSize = MaxBundleSize)
        {
            List<Bundle> bundles = new List<Bundle>();

            int bundleSize = 0;
            Bundle bundle = new Bundle();

            foreach (string removeFile in newBundle.RemovedFiles)
            {
                int fileSize = this.calculatePayloadSize(removeFile);

                if (bundleSize + fileSize > maxBundleChunkSize)
                {
                    // Save previous bundle and create new.
                    bundles.Add(bundle);

                    bundle = new Bundle();

                    bundleSize = 0;
                }

                bundle.RemovedFiles.Add(removeFile);

                bundleSize += fileSize;
            }

            // Add last created bundle in for loop to list of bundles.
            //bundles.Add(bundle);

            foreach (KeyValuePair<string, string> filePair in newBundle.Files)
            {
                int fileSize = calculateFilePairSize(filePair);

                if (bundleSize + fileSize > maxBundleChunkSize)
                {
                    // Save previous bundle and create new.
                    bundles.Add(bundle);

                    bundle = new Bundle();

                    bundleSize = 0;
                }

                bundle.Files.Add(filePair.Key, filePair.Value);

                bundleSize += fileSize;
            }

            // Add last created bundle in for loop to list of bundles.
            bundles.Add(bundle);

            return bundles;
        }

        /// <summary>
        /// Create new <see cref="Bundle"/> and get result <see cref="Bundle"/> object.
        /// </summary>
        /// <param name="newBundle">Bundle object with files data.</param>
        /// <returns>Bundle object with bundle id, missing files and upload url.</returns>
        public async System.Threading.Tasks.Task<Bundle> CreateOneChunkBundle(Bundle newBundle)
        {
            if (newBundle == null)
            {
                throw new ArgumentException("Bundle is null or empty.");
            }

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, this.snykCodeBaseUrl + BundleApiUrl);

            httpRequest.Headers.Add("Session-Token", apiToken);
            httpRequest.Version = HttpVersion.Version10;
            httpRequest.Content = new StringContent(Json.Serialize<Bundle>(newBundle), Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(httpRequest);

            string responseTest = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Json.Deserialize<Bundle>(responseTest);
            }
            else
            {
                throw new SnykCodeException(((int)response.StatusCode), responseTest);
            }
        }

        /// <summary>
        /// Returns the list of allowed extensions and configuration files for uploaded bundles.
        /// </summary>
        /// <returns><see cref="Filters"/></returns>
        public async System.Threading.Tasks.Task<Filters> GetFilters()
        {                        
            var request = new HttpRequestMessage(HttpMethod.Get, this.snykCodeBaseUrl + FiltersApiUrl);

            request.Headers.Add("Session-Token", apiToken);

            var response = await httpClient.SendAsync(request);

            string responseText = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                return Json.Deserialize<Filters>(responseText);
            }
            else
            {
                throw new SnykCodeException(((int)response.StatusCode), responseText);
            }
        }        

        /// <summary>
        /// Requests the creation of a new login session.
        /// <param name="userAgent">Represents requested client. For example, VisualStudio or VisualStudio code or other IDE.</param>
        /// /// <returns><see cref="LoginResponse"/> object.</returns>
        /// </summary>
        public async System.Threading.Tasks.Task<LoginResponse> LoginAsync(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                throw new ArgumentException("User agent is null or empty");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, this.snykCodeBaseUrl + LoginApiUrl);

            request.Headers.Add("Session-Token", apiToken);

            var response = await httpClient.SendAsync(request);

            string responseText = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                this.loginResponse = Json.Deserialize<LoginResponse>(responseText);

                return this.loginResponse;
            } 
            else
            {
                throw new SnykCodeException(((int)response.StatusCode), responseText);
            }
        }

        /// <summary>
        /// Check current session status with user token.
        /// </summary>
        /// <param name="sessionToken">User API token.</param>
        /// <returns><see cref="LoginStatus"/> object.</returns>
        public async System.Threading.Tasks.Task<LoginStatus> CheckSessionAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, this.snykCodeBaseUrl + CheckSessionApiUrl);

            request.Headers.Add("Session-Token", apiToken);

            HttpResponseMessage httpResponse = await httpClient.SendAsync(request);

            return new LoginStatus((int)httpResponse.StatusCode);
        }

        /// <summary>
        /// Calculate key value pair size in bytes. It multiply it to 2 because for UTF one char is 2 bytes.
        /// </summary>
        /// <param name="filePair">Source file pair (file path + file hash).</param>
        /// <returns>Size in bytys.</returns>
        private int calculateFilePairSize(KeyValuePair<string, string> filePair) => this.calculatePayloadSize(Json.Serialize<KeyValuePair<string, string>>(filePair));

        /// <summary>
        /// Calculate bundle size in bytes. It multiply it to 2 because for UTF one char is 2 bytes.
        /// </summary>
        /// <param name="bundle">Source bundle.</param>
        /// <returns>Size in bytys.</returns>
        private int calculateBundleSize(Bundle bundle) => this.calculatePayloadSize(Json.Serialize<Bundle>(bundle));

        /// <summary>
        /// Calculate bundle size in bytes. It multiply it to 2 because for UTF one char is 2 bytes.
        /// </summary>
        /// <param name="sourceStr">Source string.</param>
        /// <returns>Size in bytys.</returns>
        private int calculatePayloadSize(string sourceStr) => ASCIIEncoding.ASCII.GetByteCount(sourceStr) * 2;
    }      
}
