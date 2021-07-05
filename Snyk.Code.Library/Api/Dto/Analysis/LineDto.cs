namespace Snyk.Code.Library.Api.Dto.Analysis
{
    /// <summary>
    /// File line information for analysis.
    /// </summary>
    public class LineDto
    {
        /// <summary>
        /// Gets or sets a value indicating whether anaylysis line.
        /// </summary>
        public string Line { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether anaylysis line number.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether anaylysis line change.
        /// </summary>
        public string LineChange { get; set; }
    }
}
