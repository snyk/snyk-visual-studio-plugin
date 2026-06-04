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