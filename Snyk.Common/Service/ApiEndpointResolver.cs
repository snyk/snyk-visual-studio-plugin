using System;
using System.Text.RegularExpressions;
using Snyk.Common.Authentication;
using Snyk.Common.Settings;

namespace Snyk.Common.Service
{
    /// <summary>
    /// Helper class for resolve API endpoints. It's one place for all endpoint calculations.
    /// </summary>
    public class ApiEndpointResolver
    {
        private readonly ISnykOptions options;
        public const string DefaultApiEndpoint = "https://api.snyk.io";
        public const string DefaultAppEndpoint = "https://app.snyk.io";

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

            var sastUrl = string.IsNullOrEmpty(customEndpoint) ? DefaultApiEndpoint : customEndpoint;

            return !sastUrl.EndsWith("/") ? $"{sastUrl}/" : sastUrl;
        }

        /// <summary>
        /// Get correct Deeproxy URL for SaaS and Single Tenant deployment types.
        /// </summary>
        public string GetSnykCodeApiUrl()
        {
            if (IsLocalEngine())
            {
                return options.SastSettings.LocalCodeEngine.Url + "/";
            }

            var endpoint = ResolveCustomEndpoint(this.options.CustomEndpoint);

            var result = GetCustomEndpointUrlFromSnykApi(endpoint, "deeproxy");
            
            return result + "/";
        }

        /// <summary>
        /// Resolves the custom endpoint.
        /// If the endpointUrl is null or empty, then https://api.snyk.io" will be used.
        /// </summary>
        private string ResolveCustomEndpoint(string endpointUrl)
        {
            var resolvedEndpoint = string.IsNullOrEmpty(endpointUrl)
                ? DefaultApiEndpoint
                : endpointUrl.RemoveTrailingSlashes().Trim().ReplaceFirst("/v1", string.Empty);
            return resolvedEndpoint;
        }

        public static string GetCustomEndpointUrlFromSnykApi(string apiEndpoint, string subdomain)
        {
            const string regex = @"^(ap[pi]\.)?";
            if (string.IsNullOrEmpty(subdomain))
                throw new ArgumentException("subdomain must have a value to calculate the result endpoint");

            if (string.IsNullOrEmpty(apiEndpoint) || !Uri.IsWellFormedUriString(apiEndpoint, UriKind.Absolute))
                return string.Empty;

            var endpointUri = new Uri(apiEndpoint);
            
            var host = Regex.Replace(endpointUri.Host, regex, $"{subdomain}.");
            var uriBuilder = new UriBuilder(endpointUri.Scheme, host);
            return uriBuilder.ToString().RemoveTrailingSlashes();
        }

        public static string TranslateOldApiToNewApiEndpoint(string apiEndpoint)
        {
            if (apiEndpoint.Contains("https://snyk.io/api"))
                apiEndpoint = "https://app.snyk.io/api";

            if (!apiEndpoint.Contains("app.") || !apiEndpoint.RemoveTrailingSlashes().EndsWith("/api"))
                return apiEndpoint;

            var endpointUri = new Uri(apiEndpoint);

            var newEndpoint = endpointUri.Host.Replace("app.", "api.");
            var uriBuilder = new UriBuilder(endpointUri.Scheme, newEndpoint);
            return uriBuilder.ToString().RemoveTrailingSlashes();
        }

        private bool IsLocalEngine() => this.options.SastSettings?.LocalCodeEngineEnabled ?? false;
    }
}