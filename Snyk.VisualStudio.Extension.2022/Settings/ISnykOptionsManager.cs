using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.Settings
{
    public interface ISnykOptionsManager
{
    void LoadSettingsFromFile();
    void SaveSettingsToFile();
    ISnykOptions Load();
    void Save(IPersistableOptions options, bool triggerSettingsChangedEvent = true);

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