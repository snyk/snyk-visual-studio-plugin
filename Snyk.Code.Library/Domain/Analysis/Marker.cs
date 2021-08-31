namespace Snyk.Code.Library.Domain.Analysis
{
    using System.Collections.Generic;

    /// <summary>
    /// Analysis file marker information.
    /// </summary>
    public class Marker
    {
        /// <summary>
        /// Gets or sets a value indicating anaylysis marker message.
        /// </summary>
        public IList<long> MessageIndexes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis posotion in file or reference files.
        /// </summary>
        public IList<Position> Positions { get; set; }
    }
}
