﻿namespace Snyk.VisualStudio.Extension.UI.Tree
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
    }
}