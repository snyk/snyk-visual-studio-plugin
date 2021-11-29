﻿namespace Snyk.Common
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;

    /// <summary>
    /// Factory for <see cref="HttpClient"/>.
    /// </summary>
    public class HttpClientFactory
    {
        /// <summary>
        /// Create new <see cref="HttpClient"/> by base URL and API token.
        /// </summary>
        /// <param name="token">User API token.</param>
        /// <param name="baseUrl">Base URL.</param>
        /// <returns>New HttpClient instance.</returns>
        public static HttpClient NewHttpClient(string token, string baseUrl = null)
        {
            var httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
            });

            httpClient.DefaultRequestHeaders.ExpectContinue = false;

            httpClient.Timeout = TimeSpan.FromMinutes(30);

            if (!string.IsNullOrEmpty(baseUrl))
            {
                httpClient.BaseAddress = new Uri(baseUrl);
            }

            httpClient.DefaultRequestHeaders.Add("Session-Token", token);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            return httpClient;
        }
    }
}
