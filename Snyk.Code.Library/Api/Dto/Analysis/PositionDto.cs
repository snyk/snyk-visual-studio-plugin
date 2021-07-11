namespace Snyk.Code.Library.Api.Dto.Analysis
{
    using System.Collections.Generic;

    /// <summary>
    /// Analysis file position information.
    /// </summary>
    public class PositionDto
    {
        /// <summary>
        /// Gets or sets a value indicating issue rows position.
        /// </summary>
        public IList<long> Rows { get; set; }

        /// <summary>
        /// Gets or sets a value indicating issue columns position.
        /// </summary>
        public IList<long> Cols { get; set; }

        /// <summary>
        /// Gets or sets a value indicating file name.
        /// </summary>
        public string File { get; set; }
    }
}
