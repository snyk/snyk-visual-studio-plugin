namespace Snyk.VisualStudio.Extension.Shared.CLI
{
    using Newtonsoft.Json;

    /// <summary>
    /// Represents latest CLI release information.
    /// </summary>
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
        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets a value indicating whether version from TagName by removing 'v' char.
        /// </summary>
        public string CliVersion => this.TagName?.Replace("v", string.Empty);
    }
}