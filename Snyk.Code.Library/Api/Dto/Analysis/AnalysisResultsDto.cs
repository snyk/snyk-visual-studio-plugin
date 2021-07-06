namespace Snyk.Code.Library.Api.Dto.Analysis
{
    using System.Collections.Generic;

    /// <summary>
    /// Contains analysis results: files and suggestions.
    /// </summary>
    public class AnalysisResultsDto
    {
        /// <summary>
        /// Gets or sets a value indicating map suggestion id to file object.
        /// </summary>
        public IDictionary<string, SuggestionIdToFileDto> Files { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis suggestion id to suggestion object with information.
        /// </summary>
        public IDictionary<string, SuggestionDto> Suggestions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis timing.
        /// </summary>
        public TimingDto Timing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis coverage.
        /// </summary>
        public CoverageDto[] Coverage { get; set; }
    }
}
