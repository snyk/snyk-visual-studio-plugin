using Microsoft.VisualStudio.Shell;

namespace Snyk.VisualStudio.Extension
{
    /// <summary>
    /// The Visual Studio bound half of <see cref="ThreadContextEnricher"/>. Excluded from the
    /// cross-platform build (see docs/cross-platform-testing.md).
    /// </summary>
    public partial class ThreadContextEnricher
    {
        static partial void ResolveIdeThreadContext(ref string threadContext)
        {
            threadContext = ThreadHelper.CheckAccess() ? "UI Thread" : "Background Thread";
        }
    }
}
