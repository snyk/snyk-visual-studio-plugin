using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings;

public interface ISnykCliOptionsDialogPage
{
    void Initialize(ISnykServiceProvider provider);
    SnykCliOptionsUserControl SnykCliOptionsUserControl { get; }
}