using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.Settings;

public interface ISnykOptionsManager
{
    IPersistableOptions Load();
    void Save(IPersistableOptions options);
    Task<string> GetAdditionalOptionsAsync();
    Task<bool> IsScanAllProjectsAsync();
}