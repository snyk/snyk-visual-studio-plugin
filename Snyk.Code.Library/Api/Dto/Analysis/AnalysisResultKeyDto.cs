namespace Snyk.Code.Library.Api.Dto.Analysis
{
    /// <summary>
    /// Analysis result request key object.
    /// </summary>
    public class AnalysisResultKeyDto
    {
        /// <summary>
        /// Gets or sets a value indicating key type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating key hash (bundle id/bundle hash).
        /// </summary>
        public string Hash { get; set; }
    }
}
