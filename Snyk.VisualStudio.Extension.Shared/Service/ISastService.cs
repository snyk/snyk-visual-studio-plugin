namespace Snyk.VisualStudio.Extension.Shared.Service
{
    using System.Threading.Tasks;

    /// <summary>
    /// Snyk Sast service interface.
    /// </summary>
    public interface ISastService
    {
        /// <summary>
        /// Request Sast settings by Settings custom endpoint and user token.
        /// </summary>
        /// <returns>Object of <see cref="SastSettings"/>.</returns>
        Task<SastSettings> GetSastSettingsAsync();
    }
}
