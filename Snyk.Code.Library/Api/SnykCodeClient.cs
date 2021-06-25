namespace Snyk.Code.Library.Api
{
    using System;
    using System.Net.Http;
    using System.Text;
    using Snyk.Code.Library.Common;

    /// <summary>
    /// Client for SnykCode support.
    /// </summary>
    public class SnykCodeClient
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

        private readonly HttpClient httpClient;

        private LoginResponse loginResponse;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeClient"/> class.
        /// </summary>
        /// <param name="baseUrl">Base URL for deproxy.</param>
        /// <param name="token">User token.</param>
        public SnykCodeClient(string baseUrl, string token)
        {
            this.httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10),
                BaseAddress = new Uri(baseUrl),
            };

            this.httpClient.DefaultRequestHeaders.Add("Session-Token", token);
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
        /// <returns>Extended bundle object.</returns>
        public async System.Threading.Tasks.Task<Bundle> ExtendBundle(Bundle previousBundle, Bundle extendBundle)
        {
            if (previousBundle == null || string.IsNullOrEmpty(previousBundle.Id))
            {
                throw new ArgumentException("Previous Bundle is null or empty.");
            }

            if (extendBundle == null)
            {
                throw new ArgumentException("Extend Bundle is null or empty.");
            }

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Put, BundleApiUrl + "/" + previousBundle.Id);

            httpRequest.Content = new StringContent(Json.Serialize<Bundle>(extendBundle), Encoding.UTF8, "application/json");

            var response = await this.httpClient.SendAsync(httpRequest);

            string responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Json.Deserialize<Bundle>(responseText);
            }
            else
            {
                throw new SnykCodeException((int)response.StatusCode, responseText);
            }
        }

        /// <summary>
        /// Checks the status of a bundle.
        /// </summary>
        /// <param name="uploadedBundle">Bundle to check.</param>
        /// <returns
        /// >Returns the bundleId and, in case of uploaded bundles, the current missingFiles and the uploadURL.
        /// This API can be used to check if an old uploaded bundle has expired (status code 404), 
        /// or to check if there are still missing files after uploading ("Upload Files").
        /// </returns>
        public async System.Threading.Tasks.Task<Bundle> CheckBundle(Bundle uploadedBundle)
        {
            if (uploadedBundle == null || string.IsNullOrEmpty(uploadedBundle.Id))
            {
                throw new ArgumentException("Bundle is null or empty.");
            }

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, BundleApiUrl + "/" + uploadedBundle.Id);

            var response = await this.httpClient.SendAsync(httpRequest);

            string responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Json.Deserialize<Bundle>(responseText);
            }
            else
            {
                throw new SnykCodeException((int)response.StatusCode, responseText);
            }
        }

        /// <summary>
        /// Create new <see cref="Bundle"/> and get result <see cref="Bundle"/> object.
        /// </summary>
        /// <param name="newBundle">Bundle object with files data.</param>
        /// <returns>Bundle object with bundle id, missing files and upload url.</returns>
        public async System.Threading.Tasks.Task<Bundle> CreateBundle(Bundle newBundle)
        {
            if (newBundle == null)
            {
                throw new ArgumentException("Bundle is null or empty.");
            }

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, BundleApiUrl);

            httpRequest.Content = new StringContent(Json.Serialize<Bundle>(newBundle), Encoding.UTF8, "application/json");

            var response = await this.httpClient.SendAsync(httpRequest);

            string responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Json.Deserialize<Bundle>(responseText);
            }
            else
            {
                throw new SnykCodeException((int)response.StatusCode, responseText);
            }
        }

        /// <summary>
        /// Returns the list of allowed extensions and configuration files for uploaded bundles.
        /// </summary>
        /// <returns><see cref="Filters"/></returns>
        public async System.Threading.Tasks.Task<Filters> GetFilters()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, FiltersApiUrl);

            var response = await this.httpClient.SendAsync(request);

            string responseText = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                return Json.Deserialize<Filters>(responseText);
            }
            else
            {
                throw new SnykCodeException((int)response.StatusCode, responseText);
            }
        }

        /// <summary>
        /// Requests the creation of a new login session.
        /// <param name="userAgent">Represents requested client. For example, VisualStudio or VisualStudio code or other IDE.</param>
        /// /// <returns><see cref="LoginResponse"/> object.</returns>
        /// </summary>
        /// <param name="userAgent">Optional parameter with again (VisualStudio for example).</param>
        /// <returns><see cref="loginResponse"/> object.</returns>
        public async System.Threading.Tasks.Task<LoginResponse> LoginAsync(string userAgent = "")
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                throw new ArgumentException("User agent is null or empty");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, LoginApiUrl);

            var response = await this.httpClient.SendAsync(request);

            string responseText = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                this.loginResponse = Json.Deserialize<LoginResponse>(responseText);

                return this.loginResponse;
            }
            else
            {
                throw new SnykCodeException((int)response.StatusCode, responseText);
            }
        }

        /// <summary>
        /// Check current session status with user token.
        /// </summary>
        /// <param name="sessionToken">User API token.</param>
        /// <returns><see cref="LoginStatus"/> object.</returns>
        public async System.Threading.Tasks.Task<LoginStatus> CheckSessionAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, CheckSessionApiUrl);

            HttpResponseMessage httpResponse = await this.httpClient.SendAsync(request);

            return new LoginStatus((int)httpResponse.StatusCode);
        }
    }
}
