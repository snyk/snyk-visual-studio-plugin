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


    /// <summary>
    /// Get auto-determined organization.
    /// </summary>
    /// <returns>Auto-determined organization string.</returns>
    Task<string> GetAutoDeterminedOrgAsync();

    /// <summary>
    /// Save auto-determined organization.
    /// </summary>
    /// <param name="autoDeterminedOrg">Auto-determined organization string.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveAutoDeterminedOrgAsync(string autoDeterminedOrg);

    /// <summary>
    /// Get preferred organization.
    /// </summary>
    /// <returns>Preferred organization string.</returns>
    Task<string> GetPreferredOrgAsync();

    /// <summary>
    /// Save preferred organization.
    /// </summary>
    /// <param name="preferredOrg">Preferred organization string.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SavePreferredOrgAsync(string preferredOrg);

    /// <summary>
    /// Get organization set by user flag.
    /// </summary>
    /// <returns>Organization set by user flag.</returns>
    Task<bool> GetOrgSetByUserAsync();

    /// <summary>
    /// Save organization set by user flag.
    /// </summary>
    /// <param name="orgSetByUser">Organization set by user flag.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveOrgSetByUserAsync(bool orgSetByUser);

    /// <summary>
    /// Get effective organization.
    /// </summary>
    /// <returns>Effective organization string.</returns>
    Task<string> GetEffectiveOrganizationAsync();
    }
}