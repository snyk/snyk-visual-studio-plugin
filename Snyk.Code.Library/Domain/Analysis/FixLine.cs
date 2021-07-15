namespace Snyk.Code.Library.Domain.Analysis
{
    /// <summary>
    /// Contains information for suggestion fix line.
    /// </summary>
    public class FixLine
    {
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
