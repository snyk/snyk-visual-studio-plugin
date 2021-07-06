namespace Snyk.Code.Library.Api.Dto.Analysis
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Analysis file marker information.
    /// </summary>
    public class MarkerDto
    {
        /// <summary>
        /// Gets or sets a value indicating anaylysis marker message.
        /// </summary>
        [JsonPropertyName("msg")]
        public List<long> Message { get; set; }

        /// <summary>
        /// Gets or sets a value indicating anaylysis posotion in file or reference files.
        /// </summary>
        [JsonPropertyName("pos")]
        public List<PositionDto> Position { get; set; }
    }
}
