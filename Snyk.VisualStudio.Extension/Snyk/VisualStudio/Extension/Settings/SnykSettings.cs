namespace Snyk.VisualStudio.Extension.Settings
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains Snyk extension settings.
    /// </summary>
    public class SnykSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykSettings"/> class.
        /// </summary>
        public SnykSettings()
        {
            this.ProjectSettingsDict = new Dictionary<string, SnykProjectSettings>();

            this.UsageAnalyticsEnabled = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether usage analytics enabled.
        /// </summary>
        public bool UsageAnalyticsEnabled { get; set; }

        /// <summary>
        /// Gets or sets current Cli version.
        /// </summary>
        public string CurrentCliVersion { get; set; }

        /// <summary>
        /// Gets or sets Cli release last check date.
        /// </summary>
        public DateTime CliReleaseLastCheckDate { get; set; }

        /// <summary>
        /// Gets or sets project settings dictionary.
        /// </summary>
        public IDictionary<string, SnykProjectSettings> ProjectSettingsDict { get; set; }
    }
}
