namespace Snyk.Code.Library.Api.Dto.Analysis
{
    /// <summary>
    /// Analysis result request DTO.
    /// </summary>
    public class AnalysisResultRequestDto
    {
        /// <summary>
        /// Gets or sets a value indicating analysis result key object.
        /// </summary>
        public AnalysisResultKeyDto Key { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether response should be legacy json or SARIF format.
        /// </summary>
        public bool Legacy { get; set; }
    }
}
