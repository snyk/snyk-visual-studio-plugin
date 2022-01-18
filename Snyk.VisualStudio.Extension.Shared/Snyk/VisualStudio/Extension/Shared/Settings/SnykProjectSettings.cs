namespace Snyk.VisualStudio.Extension.Shared.Settings
{
    /// <summary>
    /// Contains project/solution settings for Snyk.
    /// </summary>
    public class SnykProjectSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykProjectSettings"/> class.
        /// </summary>
        public SnykProjectSettings()
        {
            this.IsAllProjectsScanEnabled = true;
        }

        /// <summary>
        /// Gets or sets additional options for project.
        /// </summary>
        public string AdditionalOptions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Is all projects scan enabled.
        /// </summary>
        public bool IsAllProjectsScanEnabled { get; set; }
    }
}