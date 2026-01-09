namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Contains project/solution settings for Snyk.
    /// </summary>
    public class SnykSolutionSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykSolutionSettings"/> class.
        /// </summary>
        public SnykSolutionSettings()
        {
            this.IsAllProjectsScanEnabled = true;
        }

        /// <summary>
        /// Gets or sets additional options for project.
        /// </summary>
        public string AdditionalOptions { get; set; }

        /// <summary>
        /// Gets or sets additional environment variables for project.
        /// </summary>
        public string AdditionalEnv { get; set; }

        /// <summary>
        /// Gets or sets organization for project.
        /// </summary>
        public string Organization { get; set; }


        /// <summary>
        /// Gets or sets the auto-determined organization from folder config.
        /// </summary>
        public string AutoDeterminedOrg { get; set; }

        /// <summary>
        /// Gets or sets the preferred organization set by user.
        /// </summary>
        public string PreferredOrg { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether organization was set by user (manual mode).
        /// </summary>
        public bool OrgSetByUser { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether Is all projects scan enabled.
        /// </summary>
        public bool IsAllProjectsScanEnabled { get; set; }
    }
}