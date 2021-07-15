namespace Snyk.Code.Library.Domain.Analysis
{
    using System.Collections.Generic;

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
        /// Gets or sets a value indicating suggestion fix lines.
        /// </summary>

        public IList<FixLine> Lines { get; set; }
    }
}
