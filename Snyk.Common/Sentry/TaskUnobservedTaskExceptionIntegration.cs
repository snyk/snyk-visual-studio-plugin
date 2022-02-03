namespace Snyk.Common.Sentry
{
    using System.Threading.Tasks;
    using global::Sentry;

    /// <summary>
    /// Snyk integration for handle TaskUnobservedTaskException.
    /// Based on code from: https://github.com/getsentry/sentry-dotnet/blob/main/src/Sentry/Integrations/TaskUnobservedTaskExceptionIntegration.cs.
    /// </summary>
    public class TaskUnobservedTaskExceptionIntegration : AbstractIntegration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskUnobservedTaskExceptionIntegration"/> class.
        /// </summary>
        public TaskUnobservedTaskExceptionIntegration()
            : base()
        {
        }

        /// <inheritdoc/>
        public override void Register(IHub hub, SentryOptions options)
        {
            this.hub = hub;
            this.appDomain.UnobservedTaskException += this.Handle;
        }

        private void Handle(object sender, UnobservedTaskExceptionEventArgs e)
        {
            if (IsNeedToHandleException(e.Exception))
            {
                e.Exception.Data[MechanismKey] = "UnobservedTaskException";

                _ = this.hub.CaptureException(e.Exception);
            }
        }
    }
}
