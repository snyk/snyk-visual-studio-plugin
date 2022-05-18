namespace Snyk.Analytics
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Snyk.Common;

    /// <summary>
    /// User for Snyk Analytics.
    /// </summary>
    public class SnykUser
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string Id { get; set; }
    }
}
