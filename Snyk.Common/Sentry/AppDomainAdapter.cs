namespace Snyk.Common.Sentry
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Wrap <see cref="AppDomain"/> instance.
    /// Based on code from: https://github.com/getsentry/sentry-dotnet/blob/main/src/Sentry/Internal/AppDomainAdapter.cs.
    /// </summary>
    public class AppDomainAdapter : IAppDomain
    {
        private AppDomainAdapter()
        {
            AppDomain.CurrentDomain.UnhandledException += this.OnUnhandledException;
            AppDomain.CurrentDomain.ProcessExit += this.OnProcessExit;

            TaskScheduler.UnobservedTaskException += this.OnUnobservedTaskException;
        }

        public event UnhandledExceptionEventHandler UnhandledException;

        public event EventHandler ProcessExit;

        public event EventHandler<UnobservedTaskExceptionEventArgs> UnobservedTaskException;

        /// <summary>
        /// Gets new instance of <see cref="AppDomainAdapter"/>.
        /// </summary>
        public static AppDomainAdapter Instance { get; } = new AppDomainAdapter();

        private void OnProcessExit(object sender, EventArgs e) => this.ProcessExit?.Invoke(sender, e);

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) => this.UnhandledException?.Invoke(this, e);

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e) => this.UnobservedTaskException?.Invoke(this, e);
    }
}
