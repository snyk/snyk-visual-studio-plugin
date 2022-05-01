namespace Snyk.Code.Library.Api.Dto.Analysis
{
    using Newtonsoft.Json;

    /// <summary>
    /// Analysis context for <see cref="AnalysisResultRequestDto"/>.
    /// </summary>
    public class AnalysisContextDto
    {
        /// <summary>
        /// Gets or sets a value indicating specific IDE.
        /// </summary>
        public string Flow { get; set; }

        /// <summary>
        /// Gets a value indicating initiator. Always 'IDE' value.
        /// </summary>
        public string Initiator => "IDE";

        /// <summary>
        /// Gets or sets a value indicating client’s snyk organization name.
        /// </summary>
        [JsonProperty("orgDisplayName")]
        public string OrgDisplayName { get; set; }
    }
}
