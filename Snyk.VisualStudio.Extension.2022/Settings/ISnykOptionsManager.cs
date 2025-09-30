using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.Settings;

public interface ISnykOptionsManager
{
    void LoadSettingsFromFile();
    void SaveSettingsToFile();
    ISnykOptions Load();
    void Save(IPersistableOptions options, bool triggerSettingsChangedEvent = true);

    /// <summary>
    /// Get CLI additional options string.
    /// </summary>
    /// <returns>string.</returns>
    Task<string> GetAdditionalOptionsAsync();

    /// <summary>
    /// Save additional options string.
    /// </summary>
    /// <param name="additionalOptions">CLI options string.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveAdditionalOptionsAsync(string additionalOptions);

    /// <summary>
    /// Get organization string.
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