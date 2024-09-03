namespace Snyk.VisualStudio.Extension.CLI
{
    using Newtonsoft.Json;

    /// <summary>
    /// Snyk Open source error object.
    /// </summary>
    public class OssError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OssError"/> class.
        /// </summary>
        public OssError() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OssError"/> class.
        /// </summary>
        /// <param name="message">Error message string.</param>
        public OssError(string message) => this.Message = message;

        /// <summary>
        /// Gets or sets a value indicating whether is success. In Json it's "ok" property.
        /// </summary>
        [JsonProperty("ok")]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether error message.
        /// </summary>
        [JsonProperty("error")]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether error path.
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }
    }
}
