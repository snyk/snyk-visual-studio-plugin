namespace Snyk.VisualStudio.Extension.Service
{
    /// <summary>
    /// The Visual Studio bound half of <see cref="ISolutionService"/>. Excluded from the
    /// cross-platform build (see docs/cross-platform-testing.md).
    /// </summary>
    public partial interface ISolutionService
    {
        /// <summary>
        /// Gets the VS solution-load event source (null until the service is initialized).
        /// Exposed on the interface so that LanguageClient and other consumers can subscribe to
        /// solution-lifecycle events without downcasting to the concrete SnykSolutionService.
        /// </summary>
        SnykVsSolutionLoadEvents SolutionEvents { get; }
    }
}
