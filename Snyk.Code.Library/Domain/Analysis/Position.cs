namespace Snyk.Code.Library.Domain.Analysis
{
    using System.Collections.Generic;

    /// <summary>
    /// Analysis file position information.
    /// </summary>
    public class Position
    {
        /// <summary>
        /// Gets or sets a value indicating issue rows position.
        /// </summary>
        public IList<long> Rows { get; set; }

        /// <summary>
        /// Gets or sets a value indicating issue columns position.
        /// </summary>
        public IList<long> Columns { get; set; }

        /// <summary>
        /// Gets or sets a value indicating file name.
        /// </summary>
        public string FileName { get; set; }
    }
}
