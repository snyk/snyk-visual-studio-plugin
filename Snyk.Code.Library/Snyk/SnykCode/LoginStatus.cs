namespace Snyk.SnykCode
{
    using System.Net;

    /// <summary>
    /// Snyk code login status.
    /// </summary>
    public class LoginStatus
    {
        private const int Succcess = 200;
        private const int LoginProcessHasNotBeenCompletedYet = 304;
        private const int MissingOrInvalidSessionToken = 401;

        private int statusCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginStatus"/> class.
        /// </summary>
        /// <param name="status">Status value.</param>
        public LoginStatus(int status) => this.statusCode = status;

        /// <summary>
        /// Gets a value indicating whether status code.
        /// </summary>
        public int StatusCode => statusCode;

        /// <summary>
        /// Gets a value indicating whether is login successful.
        /// </summary>
        public bool IsSucccess => Succcess == statusCode;

        /// <summary>
        /// Gets a value indicating whether is login process has not been completed yet.
        /// </summary>
        public bool IsLoginProcessHasNotBeenCompletedYet => LoginProcessHasNotBeenCompletedYet == statusCode;

        /// <summary>
        /// Gets a value indicating whether is missing or ivalid session token.
        /// </summary>
        public bool IsMissingOrInvalidSessionToken => MissingOrInvalidSessionToken == statusCode;

        /// <summary>
        /// Gets a value indicating whether is unauthorized.
        /// </summary>
        public bool IsUnauthorized => (int)HttpStatusCode.Unauthorized == statusCode;
    }
}
