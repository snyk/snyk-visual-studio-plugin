namespace Snyk.Code.Library.Domain.Analysis
{
    using System.Collections.Generic;

    /// <summary>
    /// Contains result information from SnykCode with file issues and suggestions how to improve source code quality.
    /// </summary>
    public class AnalysisResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnalysisResult"/> class.
        /// </summary>
        public AnalysisResult() => this.FileAnalyses = new List<FileAnalysis>();

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
        public string URL { get; set; }

        public IList<FileAnalysis> FileAnalyses { get; set; }
    }
}
