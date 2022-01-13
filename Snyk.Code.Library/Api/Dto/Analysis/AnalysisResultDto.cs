namespace Snyk.Code.Library.Api.Dto.Analysis
{
    using System.Collections.Generic;

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
        public float Progress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis url.
        /// </summary>
        public string AnalysisURL { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis timing information.
        /// </summary>
        public TimingDto Timing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis coverage information.
        /// </summary>
        public CoverageDto[] Coverage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating map suggestion id to file object.
        /// </summary>
        public IDictionary<string, SuggestionIdToFileDto> Files { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis suggestion id to suggestion object with information.
        /// </summary>
        public IDictionary<string, SuggestionDto> Suggestions { get; set; }
    }
}
