namespace Snyk.Code.Library.Domain
{
    /// <summary>
    /// SnykCode error information.
    /// </summary>
    public class SnykCodeError
    {
        /// <summary>
        /// Gets or sets message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets error code.
        /// </summary>
        public int Code { get; set; }
    }
}
