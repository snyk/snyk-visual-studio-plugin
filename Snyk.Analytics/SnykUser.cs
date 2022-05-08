using System;
using System.Net.Http;
using System.Threading.Tasks;
using Snyk.Common;

namespace Snyk.Analytics
{
    /// <summary>
    /// User for Snyk Analytics.
    /// </summary>
    public class SnykUser
    {
        private static readonly Uri SnykUserMeUri = new Uri("https://snyk.io/api/user/me/");

        public static async Task<SnykUser> GetUserAsync(string token)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, SnykUserMeUri))
                {
                    request.Headers.Add("User-Agent", "Snyk.VisualStudio.Extension");
                    request.Headers.Add("Authorization", $"token {token}");
                    request.Headers.Add("Accept", "application/json");
                    request.Headers.Add("Content-Type", "application/json");

                    var response = await httpClient.SendAsync(request);

                    return Json.Deserialize<SnykUser>(await response.Content.ReadAsStringAsync());
                }
            }

            //using (var webClient = new SnykWebClient())
            //{
            //    webClient.Headers.Add("Authorization", $"token {token}");
            //    webClient.Headers.Add("Accept", "application/json");
            //    webClient.Headers.Add("Content-Type", "application/json");

            //    var userInfoJson = await webClient.DownloadStringTaskAsync(SnykUserMeUri);

            //    //TODO - Use System.Text.Json/HttpClient.GetFromJsonAsync()
            //    return Json.Deserialize<SnykUser>(userInfoJson);
            //}
        }

        /// <summary>
        /// Gets or sets a value indicating whether Id.
        /// </summary>
        public string Id { get; set; }
    }
}
