using Snyk.VisualStudio.Extension.UI.Notifications;

namespace Snyk.VisualStudio.Extension.Download
{
    /// <summary>
    /// The Visual Studio bound half of <see cref="SnykCliDownloader"/>: reporting a failed CLI
    /// update needs the IDE info bar. Excluded from the cross-platform build (see
    /// docs/cross-platform-testing.md).
    /// </summary>
    public partial class SnykCliDownloader
    {
        static partial void ShowCliUpdateErrorInfoBar(string message) =>
            NotificationService.Instance.ShowErrorInfoBar(message);
    }
}
