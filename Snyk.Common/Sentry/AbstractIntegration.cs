namespace Snyk.Common.Sentry
{
    using System;
    using global::Sentry;
    using global::Sentry.Integrations;

    /// <summary>
    /// Snyk integration abstract class for Sentry.
    /// </summary>
    public abstract class AbstractIntegration : ISdkIntegration
    {
        protected const string HandledKey = "Sentry:Handled";

        protected const string MechanismKey = "Sentry:Mechanism";

        protected const string SnykKey = "Snyk";

        protected readonly IAppDomain appDomain;

        protected IHub hub;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractIntegration"/> class.
        /// Initialize appDoamin property with <see cref="AppDomainAdapter"/>.
        /// </summary>
        public AbstractIntegration() => this.appDomain = AppDomainAdapter.Instance;

        /// <summary>
        /// Registers this integration with the hub.
        /// </summary>
        /// <remarks>
        /// This method is invoked when the Hub is created.
        /// </remarks>
        /// <param name="hub">The hub.</param>
        /// <param name="options">The options.</param>
        public abstract void Register(IHub hub, SentryOptions options);

        /// <summary>
        /// Check is provided exception stack trace contains mentions about extension.
        /// </summary>
        /// <param name="e">Source exception.</param>
        /// <returns>True if contains mentions about Snyk extension.</returns>
        protected static bool IsNeedToHandleException(Exception e)
            => e != null && e.StackTrace != null && e.StackTrace.Contains(SnykKey);
    }
}
