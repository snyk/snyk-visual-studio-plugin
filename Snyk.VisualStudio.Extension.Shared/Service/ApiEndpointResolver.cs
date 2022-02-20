namespace Snyk.VisualStudio.Extension.Shared.Service
{
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.Settings;

    /// <summary>
    /// Helper class for resolve API endpoints. It's one place for all endpoint calculations.
    /// </summary>
    public class ApiEndpointResolver
    {
        private ISnykOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiEndpointResolver"/> class.
        /// </summary>
        /// <param name="options">Extension options.</param>
        public ApiEndpointResolver(ISnykOptions options) => this.options = options;

        /// <summary>
        /// Util method for create instance of <see cref="ApiEndpointResolver"/>.
        /// </summary>
        /// <param name="options">Extension options.</param>
        /// <returns>New ApiEndpointResolver instance.</returns>
        public static ApiEndpointResolver NewInstance(ISnykOptions options) => new ApiEndpointResolver(options);

        /// <summary>
        /// Get SnykCode Settings url.
        /// </summary>
        /// <returns>Path to Snyk Code settings url.</returns>
        public string GetSastUrl()
        {
            string customEndpoint = this.options.CustomEndpoint;

            string sastUrl = customEndpoint.IsNullOrEmpty() ? "https://snyk.io/api/" : customEndpoint;

            return !sastUrl.EndsWith("/") ? $"{sastUrl}/" : sastUrl;
        }

        /// <summary>
        /// Current logic is next:
        /// customEndpointUrl = https://dev.snyk.io/api
        /// https://dev.snyk.io/api -> https://deeproxy.dev.snyk.io/api -> https://deeproxy.dev.snyk.io/
        /// customEndpointUrl = https://snyk.io/api
        /// https://snyk.io/api -> https://deeproxy.snyk.io/api -> https://deeproxy.snyk.io/
        /// customEndpointUrl = ''
        /// https://deeproxy.snyk.io/.
        /// </summary>
        /// <returns>Result url string.</returns>
        public string GetSnykCodeApiUrl()
        {
            string customEndpointUrl = this.options.CustomEndpoint;

            if (!string.IsNullOrEmpty(customEndpointUrl) && this.IsSnykCodeSupportedEndpoint(customEndpointUrl))
            {
                return customEndpointUrl
                    .Replace("https://", "https://deeproxy.")
                    .RemoveTrailingSlashes()
                    .RemoveFromEnd("api");
            }
            else
            {
                return "https://deeproxy.snyk.io/";
            }
        }

        private bool IsSnykCodeSupportedEndpoint(string customEndpointUrl) =>
            customEndpointUrl.RemoveTrailingSlashes() == "https://dev.snyk.io/api"
                || customEndpointUrl.RemoveTrailingSlashes() == "https://snyk.io/api";
    }
}
