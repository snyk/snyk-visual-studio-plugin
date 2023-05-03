namespace Snyk.Common.Service
{
    using System;
    using Snyk.Common.Authentication;
    using Snyk.Common.Settings;

    /// <summary>
    /// Helper class for resolve API endpoints. It's one place for all endpoint calculations.
    /// </summary>
    public class ApiEndpointResolver
    {
        private readonly ISnykOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiEndpointResolver"/> class.
        /// </summary>
        /// <param name="options">Extension options.</param>
        public ApiEndpointResolver(ISnykOptions options)
        {
            this.options = options;
        }

        /// <summary>
        /// The snyk API URL without trailing backslash.
        /// </summary>
        public string SnykApiEndpoint => ResolveCustomEndpoint(this.options.CustomEndpoint);

        /// <summary>
        /// The /user/me full URL without trailing backslash.
        /// </summary>
        public string UserMeEndpoint => SnykApiEndpoint + "/v1/user/me";

        public AuthenticationType AuthenticationMethod
        {
            get
            {
                var endpoint = ResolveCustomEndpoint(this.options.CustomEndpoint);
                var endpointUri = new Uri(endpoint);
                if (endpointUri.Host.Contains("snykgov.io"))
                {
                    return AuthenticationType.OAuth;
                }

                return AuthenticationType.Token;
            }
        }

        /// <summary>
        /// Get SnykCode Settings url.
        /// </summary>
        /// <returns>Path to Snyk Code settings url.</returns>
        public string GetSnykApiEndpoint()
        {
            var customEndpoint = ResolveCustomEndpoint(this.options.CustomEndpoint);

            var sastUrl = string.IsNullOrEmpty(customEndpoint) ? "https://snyk.io/api/" : customEndpoint;

            return !sastUrl.EndsWith("/") ? $"{sastUrl}/" : sastUrl;
        }

        /// <summary>
        /// Get correct Deeproxy URL for SaaS and Single Tenant deployment types.
        /// </summary>
        public string GetSnykCodeApiUrl()
        {
            var endpoint = ResolveCustomEndpoint(this.options.CustomEndpoint);
            var uri = new Uri(endpoint);

            var result = uri.Scheme + "://" + uri.Host.Replace("api.", "deeproxy.").Replace("app.", "deeproxy.");

            if (!result.Contains("deeproxy."))
            {
                result = uri.Scheme + "://" + "deeproxy." + uri.Host;
            }

            return result + "/";
        }

        private bool IsSnykCodeAvailable(string endpointUrl)
        {
            var endpoint = ResolveCustomEndpoint(endpointUrl);
            var uri = new Uri(endpoint);
            return IsSaaS(uri) || IsSingleTenant(uri);
        }

        /// <summary>
        /// Resolves the custom endpoint.
        /// If the endpointUrl is null or empty, then https://snyk.io/api" will be used.
        /// </summary>
        private string ResolveCustomEndpoint(string endpointUrl)
        {
            var resolvedEndpoint = string.IsNullOrEmpty(endpointUrl)
                ? "https://snyk.io/api"
                : endpointUrl.RemoveTrailingSlashes().Trim().ReplaceFirst("/v1", string.Empty);
            return resolvedEndpoint;
        }

        /// <summary>
        /// Checks if the deployment type is SaaS (production or development).
        /// </summary>
        private bool IsSaaS(Uri uri) =>
            !uri.Host.StartsWith("app") && uri.Host.EndsWith("snyk.io");

        /// <summary>
        /// Checks if the deployment type is Single Tenant.
        /// </summary>
        private bool IsSingleTenant(Uri uri) =>
            uri.Host.StartsWith("app") && uri.Host.EndsWith("snyk.io");
    }
}