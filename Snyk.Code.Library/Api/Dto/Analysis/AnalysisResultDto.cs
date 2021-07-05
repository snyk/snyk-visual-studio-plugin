namespace Snyk.Code.Library.Api.Dto.Analysis
{
    /// <summary>
    /// Code analysis result information.
    /// </summary>
    public class AnalysisResultDto
    {
        /// <summary>
        /// Gets or sets a value indicating whether anaylysis status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether anaylysis progress (from 0 to 1).
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether anaylysis url.
        /// </summary>
        public string AnalysisURL { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether anaylysis information (suggestions).
        /// </summary>
        public AnalysisResultsDto AnalysisResults { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether anaylysis timing information.
        /// </summary>
        public TimingDto Timing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether anaylysis coverage information.
        /// </summary>
        public CoverageDto[] Coverage { get; set; }
    }
}
