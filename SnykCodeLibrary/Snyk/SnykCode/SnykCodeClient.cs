namespace Snyk.SnykCode
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    /// Client for SnykCode support.
    /// </summary>
    public class SnykCodeClient
    {
        private const string ApiUrl = "https://www.deepcode.ai/";

        private const string LoginApiUrl = ApiUrl + "publicapi/login";

        private const string CheckSessionApiUrl = ApiUrl + "publicapi/session";

        private readonly HttpClient httpClient = new HttpClient();

        private LoginResponse loginResponse;

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

            string payload = "{\"source\": \"" + userAgent + "\"}";

            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(LoginApiUrl, content);

            string jsonResponse = response.Content.ReadAsStringAsync().Result;

            this.loginResponse = JsonConvert.DeserializeObject<LoginResponse>(jsonResponse);

            return this.loginResponse;
        }

        /// <summary>
        /// Check current session status with user token.
        /// </summary>
        /// <param name="sessionToken">User API token.</param>
        /// <returns><see cref="LoginStatus"/> object.</returns>
        public async System.Threading.Tasks.Task<LoginStatus> CheckSessionAsync(string sessionToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, this.loginResponse.loginURL);

            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Session-Token", sessionToken));

            request.Headers.Add("Session-Token", sessionToken);

            HttpResponseMessage httpResponse = await httpClient.SendAsync(request);

            return new LoginStatus((int)httpResponse.StatusCode);
        }        
    }      
}
