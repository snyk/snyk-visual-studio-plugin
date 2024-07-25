namespace Snyk.VisualStudio.Extension.UI.Tree
{
    /// <summary>
    /// State of root tree node. 
    /// It could be enabled, disabled or disabled for organization.
    /// </summary>
    public enum RootTreeNodeState
    {
        /// <summary>
        /// Enabled state.
        /// </summary>
        Enabled,

        /// <summary>
        /// Disabled state.
        /// </summary>
        Disabled,

        /// <summary>
        /// Disabled for organization state.
        /// </summary>
        DisabledForOrganization,

        /// <summary>
        /// Scanning node state.
        /// </summary>
        Scanning,

        /// <summary>
        /// Show result details for node.
        /// </summary>
        ResultDetails,

        /// <summary>
        /// Scan error state.
        /// </summary>
        Error,

        /// <summary>
        /// Local code engine is enabled state.
        /// </summary>
        LocalCodeEngineIsEnabled,

        /// <summary>
        /// No files for SnykCode scan (No supported code available).
        /// </summary>
        NoFilesForSnykCodeScan,
    }
}
