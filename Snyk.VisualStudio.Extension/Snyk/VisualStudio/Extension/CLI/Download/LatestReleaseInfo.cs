namespace Snyk.VisualStudio.Extension.CLI
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents latest CLI release information.
    /// </summary>
    [DataContract]
    public class LatestReleaseInfo
    {
        /// <summary>
        /// Gets or sets a value indicating whether Url.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tag name.
        /// </summary>
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether name.
        /// </summary>
        public string Name { get; set; }
    }
}