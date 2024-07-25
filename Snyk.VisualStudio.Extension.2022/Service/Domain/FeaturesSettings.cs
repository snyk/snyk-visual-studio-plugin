namespace Snyk.VisualStudio.Extension.Service.Domain
{
    /// <summary>
    /// Settings for enabled/disabled features (OSS, SAST, Code quality and security).
    /// </summary>
    public class FeaturesSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether OSS enabled.
        /// </summary>
        public bool OssEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Sast on server enabled.
        /// </summary>
        public bool SastOnServerEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Code quality enabled.
        /// </summary>
        public bool CodeQualityEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Code security enabled.
        /// </summary>
        public bool CodeSecurityEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether local code engine enabled.
        /// </summary>
        public bool LocalCodeEngineEnabled { get; set; }
    }
}
