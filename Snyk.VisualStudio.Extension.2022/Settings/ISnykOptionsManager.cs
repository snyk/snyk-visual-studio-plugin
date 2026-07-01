using System.Threading.Tasks;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.Settings
{
    public interface ISnykOptionsManager
{
    void LoadSettingsFromFile();
    void SaveSettingsToFile();
    ISnykOptions Load();
    /// <param name="updateOverrideTracker">
    /// When true (default, user-initiated saves): calls ApplyUserEdits and persists ChangedConfigKeys.
    /// When false (LS-originated / system saves): skips tracker mutation so LS-pushed values are
    /// never recorded as user overrides, and leaves ChangedConfigKeys on disk exactly as it was.
    /// </param>
    /// <param name="editedKeys">
    /// The set of pflag keys the user actually changed in this save action. Only these keys are
    /// marked / unmarked in the tracker (edit-delta). Null or empty → no keys are marked (safe
    /// default: org-pushed values already in Options are never accidentally frozen). Only consulted
    /// when <paramref name="updateOverrideTracker"/> is true.
    /// </param>
    /// <param name="resetKeys">
    /// The set of global pflag keys the user reset to default in this save action (posted as explicit
    /// JSON null by the "Reset overrides" button). For each, the tracker un-marks the local override
    /// and enqueues a <c>{value:null, changed:true}</c> reset signal for the LS (IDE-2152). Disjoint
    /// from <paramref name="editedKeys"/>: a key is either edited-to-a-value or reset, never both.
    /// Null or empty → no resets. Only consulted when <paramref name="updateOverrideTracker"/> is true.
    /// </param>
    void Save(IPersistableOptions options, bool triggerSettingsChangedEvent = true,
              bool updateOverrideTracker = true,
              System.Collections.Generic.IReadOnlyCollection<string> editedKeys = null,
              System.Collections.Generic.IReadOnlyCollection<string> resetKeys = null);

    /// <summary>
    /// The user-override tracker singleton owned by this manager. Used by
    /// <see cref="LsSettingsV25"/> to set the <c>changed</c> flag on each ConfigSetting (IDE-2152).
    /// </summary>
    IUserOverrideTracker OverrideTracker { get; }

    /// <summary>
    /// Commit reset keys confirmed-delivered to the Language Server (IDE-2152 fix #2): removes exactly
    /// the given keys from the tracker's pending-reset queue AND re-persists the shrunken set to disk,
    /// so a delivered reset is not re-sent after a restart. The single commit entry point — the
    /// config-send path calls this rather than the tracker directly, keeping persistence and the
    /// in-memory queue in sync. No-op on null/empty.
    /// </summary>
    void CommitPendingResets(System.Collections.Generic.IReadOnlyCollection<string> sentKeys);

    /// <summary>
    /// Migrate the legacy per-solution settings entry for the given solution folder (if any) into the
    /// folder config model, then drop the legacy entry. Best-effort and idempotent. See
    /// <see cref="SnykOptionsManager.MigrateLegacySolutionSettings"/>.
    /// </summary>
    /// <param name="solutionFolderPath">The open solution folder path.</param>
    /// <returns><c>true</c> if an entry was migrated.</returns>
    bool MigrateLegacySolutionSettings(string solutionFolderPath);

    /// <summary>
    /// Get global organization string.
    /// </summary>
    /// <returns>string.</returns>
    Task<string> GetOrganizationAsync();

    /// <summary>
    /// Save organization string.
    /// </summary>
    /// <param name="organization">Organization string.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveOrganizationAsync(string organization);
    }
}