namespace Snyk.VisualStudio.Extension
{
    /// <summary>
    /// Sast local code engine settings.
    /// </summary>
    public class LocalCodeEngine
    {
        /// <summary>
        /// Gets or sets a value indicating whether cloud upload allowed.
        /// </summary>
        public bool AllowCloudUpload { get; set; }

        /// <summary>
        /// Gets or sets a url value.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Local code engine enabled or not on server.
        /// </summary>
        public bool Enabled { get; set; }
    }
}
