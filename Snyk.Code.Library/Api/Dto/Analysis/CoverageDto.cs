namespace Snyk.Code.Library.Api.Dto.Analysis
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Analysis result coverage information.
    /// </summary>
    public class CoverageDto
    {
        /// <summary>
        /// Gets or sets a value indicating whether anaylysis source language.
        /// </summary>
        [JsonPropertyName("lang")]
        public string Langguage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether anaylysis is supported language.
        /// </summary>
        public bool IsSupported { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether anaylysis files count.
        /// </summary>
        [JsonPropertyName("files")]
        public int FilesCount { get; set; }
    }
}
