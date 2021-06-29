namespace Snyk.Code.Library.Api
{
    using System;

    /// <summary>
    /// Contains SnykCode exception information. Http response status, error message.
    /// </summary>
    public class SnykCodeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeException"/> class.
        /// </summary>
        /// <param name="message">Error message.</param>
        public SnykCodeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeException"/> class.
        /// </summary>
        /// <param name="statusCode">Response status code.</param>
        /// <param name="message">Error message.</param>
        public SnykCodeException(int statusCode, string message) 
            : base(message) => this.StatusCode = statusCode;

        /// <summary>
        /// Gets or sets a value indicating whether status code.
        /// </summary>
        public int StatusCode { get; set; }
    }
}
