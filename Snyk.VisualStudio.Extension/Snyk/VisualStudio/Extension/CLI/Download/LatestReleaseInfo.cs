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
        [JsonPropertyName("url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Id.
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tag name.
        /// </summary>
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets a value indicating whether version from TagName by removing 'v' char.
        /// </summary>
        public string CliVersion => this.TagName?.Replace("v", string.Empty);
    }
}