namespace Snyk.VisualStudio.Extension.Shared.CLI
{
    using Newtonsoft.Json;

    /// <summary>
    /// Snyk Open source error object.
    /// </summary>
    public class CliError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CliError"/> class.
        /// </summary>
        public CliError() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CliError"/> class.
        /// </summary>
        /// <param name="message">Error message string.</param>
        public CliError(string message) => this.Message = message;

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
