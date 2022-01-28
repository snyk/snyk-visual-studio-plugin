namespace Snyk.VisualStudio.Extension.Shared.Service
{
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;
    using Sentry;
    using Snyk.Common;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Service incapsulate work with Sentry reporting.
    /// </summary>
    public class SentryService : ISentryService
    {
        private ISnykServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SentryService"/> class.
        /// </summary>
        /// <param name="serviceProvider">Service provider instance.</param>
        public SentryService(ISnykServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

        /// <inheritdoc/>
        public async Task SetupAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var shell = await this.serviceProvider.GetServiceAsync(typeof(SVsShell)) as IVsShell;
            shell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out object vsVersion);

            SentrySdk.ConfigureScope(scope =>
            {
                scope.User = new User { Id = this.serviceProvider.AnalyticsService.UserIdAsHash, };

                scope.SetTag("vs.version", vsVersion.ToString());
                scope.SetTag("vs.edition", this.serviceProvider.DTE.Edition);
            });
        }

        /// <inheritdoc/>
        public void SetSolutionType(SolutionType solutionType)
        {
            LogManager.SentryConfiguration.BeforeSend = sentryEvent =>
            {
                sentryEvent.SetTag("vs.project.type", solutionType.ToString());

                return sentryEvent;
            };
        }
    }
}
