namespace Snyk.Code.Library.Api.Dto.Analysis
{
    using System.Collections.Generic;

    /// <summary>
    /// Analysis result file information.
    /// </summary>
    public class FileDto
    {
        /// <summary>
        /// Gets or sets a value indicating issue rows.
        /// </summary>
        public int[] Rows { get; set; }

        /// <summary>
        /// Gets or sets a value indicating issue columns.
        /// </summary>
        public int[] Cols { get; set; }

        /// <summary>
        /// Gets or sets a value indicating issue marker in this file or other reference files.
        /// </summary>
        public IList<MarkerDto> Markers { get; set; }
    }
}
