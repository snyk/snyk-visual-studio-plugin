namespace Snyk.VisualStudio.Extension.Shared.Service
{
    using System;
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
        /// Get SnykCode Settings url.
        /// </summary>
        /// <returns>Path to Snyk Code settings url.</returns>
        public string GetSnykApiEndpoint()
        {
            string customEndpoint = this.options.CustomEndpoint;

            string sastUrl = string.IsNullOrEmpty(customEndpoint) ? "https://snyk.io/api/" : customEndpoint;

            return !sastUrl.EndsWith("/") ? $"{sastUrl}/" : sastUrl;
        }

        /// <summary>
        /// Get correct Deeproxy URL for SaaS and Single Tenant deployment types.
        /// </summary>
        public string GetSnykCodeApiUrl()
        {
            string endpoint = this.ResolveCustomEndpoint(this.options.CustomEndpoint);
            Uri uri = new Uri(endpoint);

            if (this.IsSaaS(uri))
            {
                return endpoint.Replace("https://", "https://deeproxy.").RemoveFromEnd("api");
            }
            else if (this.IsSingleTenant(uri))
            {
                return endpoint.Replace("registry-web", "deeproxy").RemoveFromEnd("api");
            }
            else
            {
                return "https://deeproxy.snyk.io/";
            }
        }

        private bool IsSnykCodeAvailable(string endpointUrl)
        {
            string endpoint = ResolveCustomEndpoint(endpointUrl);
            Uri uri = new Uri(endpoint);
            return this.IsSaaS(uri) || this.IsSingleTenant(uri);
        }

        /// <summary>
        /// Resolves the custom endpoint.
        ///
        /// If the endpointUrl is null or empty, then https://snyk.io/api" will be used.
        /// </summary>
        private string ResolveCustomEndpoint(string endpointUrl) =>
            string.IsNullOrEmpty(endpointUrl) ? "https://snyk.io/api": endpointUrl.RemoveTrailingSlashes();

        /// <summary>
        /// Checks if the deployment type is SaaS (production or development).
        /// </summary>
        private bool IsSaaS(Uri uri) =>
            uri.Host.EndsWith("snyk.io");

        /// <summary>
        /// Checks if the deployment type is Single Tenant.
        /// </summary>
        private bool IsSingleTenant(Uri uri) =>
            uri.Host.Contains("registry-web") && uri.Host.EndsWith("snyk-internal.net");
    }
}
