namespace Snyk.Code.Library.Api.Dto.Analysis
{
    /// <summary>
    /// Code analysis result information.
    /// </summary>
    public class AnalysisResultDto
    {
        /// <summary>
        /// Gets or sets a value indicating anaylysis status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis progress (from 0 to 1).
        /// </summary>
        public long Progress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis url.
        /// </summary>
        public string AnalysisURL { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis information (suggestions).
        /// </summary>
        public AnalysisResultsDto AnalysisResults { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis timing information.
        /// </summary>
        public TimingDto Timing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis coverage information.
        /// </summary>
        public CoverageDto[] Coverage { get; set; }
    }
}
