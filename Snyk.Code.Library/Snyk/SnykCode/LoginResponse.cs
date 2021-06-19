namespace Snyk.SnykCode
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// SnykCode login response result.
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether Session token.
        /// </summary>
        public string SessionToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether login URL.
        /// </summary>
        public string LoginURL { get; set; }

        /// <summary>
        /// Gets a value indicating whether is success login.
        /// Check is error null or empty and session token not null or empty.
        /// </summary>
        public bool IsSuccess => string.IsNullOrEmpty(Error) && !string.IsNullOrEmpty(SessionToken);

        private string Error { get; set; }
    }
}
