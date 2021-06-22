namespace Snyk.Code.Library.SnykCode
{    
    using System;
    using System.Net.Http;
    using Snyk.Code.Library.Common;

    /// <summary>
    /// Client for SnykCode support.
    /// </summary>
    public class SnykCodeClient
    {
        private const string LoginApiUrl = "publicapi/login";

        private const string CheckSessionApiUrl = "publicapi/session";

        private const string FiltersApiUrl = "publicapi/filters";

        private readonly HttpClient httpClient = new HttpClient();

        private LoginResponse loginResponse;
        
        private string snykCodeBaseUrl;

        private string apiToken;

        /// <summary>
        /// 
        /// </summary>
        public SnykCodeClient()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeClient"/> class.
        /// </summary>
        /// <param name="apiToken">User API token for authentication</param>
        public SnykCodeClient(string apiToken)
        {
            this.apiToken = apiToken;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeClient"/> class.
        /// </summary>
        /// <param name="baseUrl">Deproxy base URL.</param>
        /// <param name="token">User API token for authentication</param>
        public SnykCodeClient(string baseUrl, string token)
        {
            this.snykCodeBaseUrl = baseUrl;
            this.apiToken = token;
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
    }      
}
