namespace Snyk.SnykCode
{
    /// <summary>
    /// SnykCode login response result.
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether Session token.
        /// </summary>
        public string sessionToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether login URL.
        /// </summary>
        public string loginURL { get; set; }

        /// <summary>
        /// Gets a value indicating whether is success login.
        /// Check is error null or empty and session token not null or empty.
        /// </summary>
        public bool IsSuccess => string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(sessionToken);

        private string error { get; set; }
    }
}
