namespace Snyk.Common.Sentry
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for <see cref="AppDomainAdapter"/>.
    /// Based on code from: https://github.com/getsentry/sentry-dotnet/blob/main/src/Sentry/Internal/AppDomainAdapter.cs.
    /// </summary>
    public interface IAppDomain
    {
        event UnhandledExceptionEventHandler UnhandledException;

        event EventHandler ProcessExit;

        event EventHandler<UnobservedTaskExceptionEventArgs> UnobservedTaskException;
    }
}
