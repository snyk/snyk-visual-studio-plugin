using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings;

public interface ISnykExperimentalDialogPage
{
    void Initialize(ISnykServiceProvider provider);
}