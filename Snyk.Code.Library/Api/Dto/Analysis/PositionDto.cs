namespace Snyk.Code.Library.Api.Dto.Analysis
{
    using System.Collections.Generic;

    /// <summary>
    /// Analysis file position information.
    /// </summary>
    public class PositionDto
    {
        /// <summary>
        /// Gets or sets a value indicating whether issue rows position.
        /// </summary>
        public List<long> Rows { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether issue columns position.
        /// </summary>
        public List<long> Cols { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether file name.
        /// </summary>
        public string File { get; set; }
    }
}
