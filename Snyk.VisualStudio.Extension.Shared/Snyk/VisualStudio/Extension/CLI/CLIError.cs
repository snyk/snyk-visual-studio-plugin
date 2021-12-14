namespace Snyk.VisualStudio.Extension.CLI
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Snyk Open source error object.
    /// </summary>
    [DataContract]
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
        [JsonPropertyName("ok")]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether error message.
        /// </summary>
        [JsonPropertyName("error")]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether error path.
        /// </summary>
        public string Path { get; set; }
    }
}
