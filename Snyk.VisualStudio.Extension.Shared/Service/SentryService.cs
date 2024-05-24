namespace Snyk.VisualStudio.Extension.Shared.Service
{
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
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
                scope.User = new SentryUser { Id = this.serviceProvider.Options.SnykUser.IdHash };

                scope.SetTag("vs.version", vsVersion.ToString());
                scope.SetTag("vs.edition", this.serviceProvider.DTE.Edition);
            });

            // disable sentry if no usage analytics or fedramp 
            if (!this.serviceProvider.Options.UsageAnalyticsEnabled || this.serviceProvider.Options.IsFedramp())
            {
                DiscardEventCallback();
            }
        }

        /// <inheritdoc/>
        public void SetSolutionType(SolutionType solutionType)
        {
            if (!this.serviceProvider.Options.UsageAnalyticsEnabled || this.serviceProvider.Options.IsFedramp())
            {
                DiscardEventCallback();
            }
            else
            {
                LogManager.SentryConfiguration.SetBeforeSend(sentryEvent =>
                {
                    sentryEvent.SetTag("vs.project.type", solutionType.ToString());
                    return sentryEvent;
                });
            }
        }

        private static void DiscardEventCallback()
        {
            LogManager.SentryConfiguration.SetBeforeSend(_ => { return null; });
        }
    }
}
