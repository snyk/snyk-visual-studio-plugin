namespace Snyk.Code.Library.Domain.Analysis
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains suggestion information for improve code issue.
    /// </summary>
    public class Suggestion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Suggestion"/> class.
        /// </summary>
        public Suggestion() => this.Fixes = new List<SuggestionFix>();

        /// <summary>
        /// Gets or sets a value indicating anaylysis suggestion id. Id for this (local) result.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets tuple of rows.
        /// </summary>
        public Tuple<int, int> Rows { get; set; }

        /// <summary>
        /// Gets or sets tuple of columns.
        /// </summary>
        public Tuple<int, int> Columns { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis rule.
        /// </summary>
        public string Rule { get; set; }

        /// <summary>
        /// Gets or sets a value indicating suggestion message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis severity value (from 1 to 4).
        /// </summary>
        public int Severity { get; set; }

        /// <summary>
        /// Gets or sets position markers.
        /// </summary>
        public IList<Marker> Markers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis categories.
        /// </summary>
        public IList<string> Categories { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis tags.
        /// </summary>
        public IList<string> Tags { get; set; }

        /// <summary>
        /// Gets or sets a value indicating suggestion title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets a value indicating analysis CWE list.
        /// </summary>
        public IList<string> Cwe { get; set; }

        /// <summary>
        /// Gets or sets a value indicating suggestion description.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets a value indicating suggestion examples.
        /// </summary>
        public IList<string> ExampleCommitDescriptions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating suggestion list of proposed fixes and examples.
        /// </summary>
        public IList<SuggestionFix> Fixes { get; set; }

        /// <summary>
        /// Get row line and title (if title is null it use message).
        /// </summary>
        /// <returns>Title for display.</returns>
        public string GetDisplayTitleWithLineNumber() => "Line " + this.Rows.Item1 + ": " + this.GetDisplayTitle();

        /// <summary>
        /// Get title. If title is null or empty it will return message as title.
        /// </summary>
        /// <returns>Title for display.</returns>
        public string GetDisplayTitle()
        {
            string title = string.Empty;

            if (string.IsNullOrEmpty(this.Title))
            {
                title += this.Message;
            }
            else
            {
                title += this.Title;
            }

            return title;
        }
    }
}
