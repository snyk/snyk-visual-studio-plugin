namespace Snyk.Code.Library.Api.Dto.Analysis
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Analysis file marker information.
    /// </summary>
    public class MarkerDto
    {
        /// <summary>
        /// Gets or sets a value indicating anaylysis marker message.
        /// </summary>
        [JsonProperty("msg")]
        public IList<long> MessageIndexes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis posotion in file or reference files.
        /// </summary>
        [JsonProperty("pos")]
        public IList<PositionDto> Positions { get; set; }
    }
}
