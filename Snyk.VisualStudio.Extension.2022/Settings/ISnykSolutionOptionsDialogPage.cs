using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings;

public interface ISnykSolutionOptionsDialogPage
{
    void Initialize(ISnykServiceProvider provider);
    SnykSolutionOptionsUserControl SnykSolutionOptionsUserControl { get; }
}