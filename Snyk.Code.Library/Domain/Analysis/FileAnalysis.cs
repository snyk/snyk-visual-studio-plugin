namespace Snyk.Code.Library.Domain.Analysis
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains Analysis results for file.
    /// </summary>
    public class FileAnalysis
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileAnalysis"/> class.
        /// </summary>
        public FileAnalysis() => this.Suggestions = new List<Suggestion>();

        /// <summary>
        /// Gets or sets a value indicating file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating list of suggestions for file.
        /// </summary>
        public IList<Suggestion> Suggestions { get; set; }
    }
}
