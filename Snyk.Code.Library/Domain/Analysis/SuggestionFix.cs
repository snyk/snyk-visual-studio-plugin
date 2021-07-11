namespace Snyk.Code.Library.Domain.Analysis
{
    /// <summary>
    /// Contains suggestion fix information how to improve code quality.
    /// </summary>
    public class SuggestionFix
    {
        /// <summary>
        /// Gets or sets a value indicating commit url.
        /// </summary>
        public string CommitURL { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis line.
        /// </summary>
        public string Line { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis line number.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis line change.
        /// </summary>
        public string LineChange { get; set; }
    }
}
