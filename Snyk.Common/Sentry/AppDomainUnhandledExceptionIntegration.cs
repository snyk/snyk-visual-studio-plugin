namespace Snyk.Common.Sentry
{
    using System;
    using global::Sentry;

    /// <summary>
    /// Snyk integration for handle AppDomainUnhandledException.
    /// Based on the code from: https://github.com/getsentry/sentry-dotnet/blob/main/src/Sentry/Integrations/AppDomainUnhandledExceptionIntegration.cs.
    /// </summary>
    public class AppDomainUnhandledExceptionIntegration : AbstractIntegration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppDomainUnhandledExceptionIntegration"/> class.
        /// </summary>
        public AppDomainUnhandledExceptionIntegration()
            : base()
        {
        }

        /// <inheritdoc/>
        public override void Register(IHub hub, SentryOptions options)
        {
            this.hub = hub;
            this.appDomain.UnhandledException += this.Handle;
        }

        private void Handle(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex && IsNeedToHandleException(ex))
            {
                ex.Data[HandledKey] = false;
                ex.Data[MechanismKey] = "AppDomain.UnhandledException";

                _ = this.hub.CaptureException(ex);
            }

            if (e.IsTerminating)
            {
                (this.hub as IDisposable)?.Dispose();
            }
        }
    }
}
