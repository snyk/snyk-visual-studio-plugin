namespace Snyk.Common.Service
{
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Snyk Sast service interface.
    /// </summary>
    public interface ISnykApiService
    {
        Task<SnykUser> GetUserAsync();

        /// <summary>
        /// Request Sast settings by Settings custom endpoint and user token.
        /// </summary>
        /// <returns>Object of <see cref="SastSettings"/>.</returns>
        Task<SastSettings> GetSastSettingsAsync();

        /// <summary>
        /// Send sast settings request to server.
        /// </summary>
        /// <returns>HttpResponseMessage response object.</returns>
        Task<HttpResponseMessage> SendSastSettingsRequestAsync();
    }
}
