namespace Snyk.Code.Library.Api.Dto.Analysis
{
    /// <summary>
    /// Example commit fix for suggestion.
    /// </summary>
    public class ExampleCommitFixDto
    {
        /// <summary>
        /// Gets or sets a value indicating commit url.
        /// </summary>
        public string CommitURL { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis line information.
        /// </summary>
        public LineDto[] Lines { get; set; }
    }
}
