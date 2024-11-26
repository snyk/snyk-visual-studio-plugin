using Snyk.VisualStudio.Extension.UI.Toolwindow;

namespace Snyk.VisualStudio.Extension
{
    using System;
    using Snyk.VisualStudio.Extension.UI;

    /// <summary>
    /// Contains all Guids for Snyk extension.
    /// </summary>
    public class SnykGuids
    {
        /// <summary>
        /// Toolbar id.
        /// </summary>
        public const int SnykToolbarId = 0x501;

        /// <summary>
        /// Scan command id.
        /// </summary>
        public const int RunScanCommandId = 0x503;

        /// <summary>
        /// Command id.
        /// </summary>
        public const int StopCommandId = 0x504;

        /// <summary>
        /// Clean command id.
        /// </summary>
        public const int CleanCommandId = 0x505;

        /// <summary>
        /// Options command id.
        /// </summary>
        public const int OptionsCommandId = 0x506;

        public const int OpenToolWindowCommandId = SnykToolWindowCommand.CommandId;

        /// <summary>
        /// VS package command set.
        /// </summary>
        public static readonly Guid SnykVSPackageCommandSet = new Guid("{31b6f1bd-8317-4d93-b023-b60f667b9e76}");
    }
}
