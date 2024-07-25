namespace Snyk.VisualStudio.Extension.Service
{
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Service incapsulate work with Sentry reporting.
    /// </summary>
    public interface ISentryService
    {
        /// <summary>
        /// Setup Sentry scope with Visual Studio versions and edition.
        /// </summary>
        /// <returns>Task.</returns>
        Task SetupAsync();

        /// <summary>
        /// Set current solution type <see cref="SolutionType"/> for Sentry events.
        /// </summary>
        /// <param name="solutionType">Current solution type.</param>
        void SetSolutionType(SolutionType solutionType);
    }
}
