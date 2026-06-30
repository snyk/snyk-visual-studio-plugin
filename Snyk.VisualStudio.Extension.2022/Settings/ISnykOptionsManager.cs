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
    void Save(IPersistableOptions options, bool triggerSettingsChangedEvent = true,
              bool updateOverrideTracker = true,
              System.Collections.Generic.IReadOnlyCollection<string> editedKeys = null);

    /// <summary>
    /// The user-override tracker singleton owned by this manager. Used by
    /// <see cref="LsSettingsV25"/> to set the <c>changed</c> flag on each ConfigSetting (IDE-2152).
    /// </summary>
    IUserOverrideTracker OverrideTracker { get; }

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