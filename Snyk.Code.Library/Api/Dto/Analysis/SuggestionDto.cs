namespace Snyk.Code.Library.Api.Dto.Analysis
{
    using System.Collections.Generic;

    /// <summary>
    /// Analysis suggestion information.
    /// </summary>
    public class SuggestionDto
    {
        /// <summary>
        /// Gets or sets a value indicating whether anaylysis suggestion id. Id for this (local) result.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether anaylysis rule.
        /// </summary>
        public string Rule { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether suggestion message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether anaylysis severity value (from 1 to 4).
        /// </summary>
        public int Severity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether anaylysis categories.
        /// </summary>
        public IEnumerable<string> Categories { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether anaylysis tags.
        /// </summary>
        public IEnumerable<string> Tags { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether suggestion title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether analysis CWE list.
        /// </summary>
        public IEnumerable<string> Cwe { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether suggestion description.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether suggestion examples.
        /// </summary>
        public IEnumerable<string> ExampleCommitDescriptions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether suggestion fixes.
        /// </summary>
        public IEnumerable<ExampleCommitFixDto> ExampleCommitFixes { get; set; }
    }
}
