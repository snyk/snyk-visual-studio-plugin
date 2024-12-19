using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.Settings;

public interface ISnykOptionsManager
{
    void LoadSettingsFromFile();
    void SaveSettingsToFile();
    IPersistableOptions Load();
    void Save(IPersistableOptions options);

    /// <summary>
    /// Get is all projects enabled.
    /// </summary>
    /// <returns>Bool.</returns>
    Task<bool> GetIsAllProjectsEnabledAsync();

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
    /// Sace is all projects scan enabled.
    /// </summary>
    /// <param name="isAllProjectsEnabled">Bool param.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveIsAllProjectsScanEnabledAsync(bool isAllProjectsEnabled);
}