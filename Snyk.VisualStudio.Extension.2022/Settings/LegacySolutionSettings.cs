// ABOUTME: Transitional DTO for the legacy per-solution settings block (solutionSettingsDict).
// ABOUTME: Read-only / migration-only — see LegacySolutionSettingsMigrator and SnykSettings.SolutionSettingsDict.

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Legacy per-solution settings, as persisted in the removed <c>solutionSettingsDict</c> section
    /// of settings.json (IDE-1651). This type exists ONLY so that section can still be deserialized
    /// and faithfully re-serialized while it is migrated, one solution at a time, into the folder
    /// config model owned by the Language Server. No feature code reads or writes it; it is mutated
    /// only by <see cref="LegacySolutionSettingsMigrator"/> / <see cref="SnykOptionsManager"/> as
    /// entries are migrated and removed.
    /// <para>
    /// Mirrors the deleted <c>SnykSolutionSettings</c> field-for-field so an upgrading user's
    /// un-migrated entries round-trip unchanged. <c>IsAllProjectsScanEnabled</c> is preserved only
    /// for faithful round-tripping — it is intentionally NOT migrated, as it has been dead since
    /// scanning moved to the Language Server (no reader, no CLI argument).
    /// </para>
    /// </summary>
    public class LegacySolutionSettings
    {
        public string AdditionalOptions { get; set; }

        public string AdditionalEnv { get; set; }

        public string Organization { get; set; }

        public string AutoDeterminedOrg { get; set; }

        public string PreferredOrg { get; set; }

        public bool OrgSetByUser { get; set; }

        public bool IsAllProjectsScanEnabled { get; set; }
    }
}
