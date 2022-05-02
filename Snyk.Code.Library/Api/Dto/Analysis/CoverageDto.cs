namespace Snyk.Code.Library.Api.Dto.Analysis
{
    using Newtonsoft.Json;

    /// <summary>
    /// Analysis result coverage information.
    /// </summary>
    public class CoverageDto
    {
        /// <summary>
        /// Gets or sets a value indicating anaylysis source language.
        /// </summary>
        [JsonProperty("lang")]
        public string Langguage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether anaylysis is supported language.
        /// </summary>
        public bool IsSupported { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis files count.
        /// </summary>
        [JsonProperty("files")]
        public int FilesCount { get; set; }
    }
}
