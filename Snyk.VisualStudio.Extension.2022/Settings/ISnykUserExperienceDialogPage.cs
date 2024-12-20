using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings;

public interface ISnykUserExperienceDialogPage
{
    void Initialize(ISnykServiceProvider provider);
}