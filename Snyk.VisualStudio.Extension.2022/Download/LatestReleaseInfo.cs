namespace Snyk.VisualStudio.Extension.Download
{
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
        /// Gets or sets a value indicating whether name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether version.
        /// </summary>
        public string Version { get; set; }
    }
}