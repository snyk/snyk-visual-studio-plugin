namespace Snyk.VisualStudio.Extension.Shared.Settings
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
        }

        /// <summary>
        /// Gets or sets a value indicating whether usage analytics enabled.
        /// </summary>
        public bool UsageAnalyticsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets current Cli version.
        /// </summary>
        public string CurrentCliVersion { get; set; }

        /// <summary>
        /// Gets or sets Sentry anonymous user id.
        /// </summary>
        public string AnonymousId { get; set; }

        /// <summary>
        /// Gets or sets Cli release last check date.
        /// </summary>
        public DateTime CliReleaseLastCheckDate { get; set; }

        /// <summary>
        /// Gets or sets solution settings dictionary.
        /// </summary>
        public IDictionary<int, SnykSolutionSettings> SolutionSettingsDict { get; set; } = new Dictionary<int, SnykSolutionSettings>();

        /// <summary>
        /// Gets or sets a value indicating whether snyk code security enabled.
        /// </summary>
        public bool SnykCodeSecurityEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether snyk code quality enabled.
        /// </summary>
        public bool SnykCodeQualityEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether oss enabled.
        /// </summary>
        public bool OssEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether binaries auto update is enabled.
        /// </summary>
        public bool BinariesAutoUpdateEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the value of the custom CLI path.
        /// </summary>
        public string CustomCliPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an array of workspace trusted folders.
        /// </summary>
        public ISet<string> TrustedFolders { get; set; } = new HashSet<string>();
    }
}