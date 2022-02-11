﻿namespace Snyk.VisualStudio.Extension.Shared.Service
{
    /// <summary>
    /// Sast settings.
    /// </summary>
    public class SastSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether SastEnabled or not on server.
        /// </summary>
        public bool SastEnabled { get; set; }

        /// <summary>
        /// Gets or sets local code engine settings.
        /// </summary>
        public LocalCodeEngine LocalCodeEngine { get; set; }
    }
}
