namespace Snyk.Code.Library.SnykCode
{
    using System;

    /// <summary>
    /// Contains SnykCode exception information. Http response status, error message.
    /// </summary>
    public class SnykCodeException : Exception
    {
        public SnykCodeException(int statusCode, string message) : base(message)
        {
            this.StatusCode = statusCode;
        }

        public int StatusCode { get; set; }
    }
}
