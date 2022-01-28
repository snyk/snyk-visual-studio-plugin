namespace Snyk.Common
{
    using System;
    using System.IO;
    using Sentry;
    using Sentry.Serilog;
    using Serilog;
    using Serilog.Core;
    using Serilog.Events;
    using Serilog.Exceptions;

    /// <summary>
    /// Logger manager for create logger per class.
    /// </summary>
    public static class LogManager
    {
        private const string OutputTemplate =
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{ProcessId:00000}] {Level:u4} [{ThreadId:00}] {ShortSourceContext,-25} {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// 10 Mb.
        /// </summary>
        private const int LogFileSize = 10485760;

        #if DEBUG
        private static LogEventLevel defaultLoggingLevel = LogEventLevel.Debug;
        #else
        private static LogEventLevel defaultLoggingLevel = LogEventLevel.Information;
        #endif

        private static LoggingLevelSwitch loggingLevelSwitch = new LoggingLevelSwitch(defaultLoggingLevel);

        private static SentrySerilogOptions sentryConfiguration;

        private static Lazy<Logger> Logger { get; } = new Lazy<Logger>(CreateLogger);

        /// <summary>
        /// Create Logger instance for provided type.
        /// </summary>
        /// <typeparam name="T">Type of class.</typeparam>
        /// <returns><see cref="ILogger"/> implementation for class.</returns>
        public static ILogger ForContext<T>() => ForContext(typeof(T));

        /// <summary>
        /// Set Sentry BeforeSend hook function.
        /// </summary>
        /// <param name="beforeSendHookFunc">Function for execute before Sentry send events.</param>
        public static void SetSentryBeforeSendHook(Func<SentryEvent, SentryEvent> beforeSendHookFunc)
        {
            sentryConfiguration.BeforeSend = beforeSendHookFunc;
        }

        /// <summary>
        /// Check is sentry configuration already initialized.
        /// If sentryConfiguration not null it means logger configuration already created and ready to use.
        /// </summary>
        /// <returns>True if logger configuration already created</returns>
        public static bool IsInitialized() => sentryConfiguration != null;

        private static Logger CreateLogger() => new LoggerConfiguration()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithExceptionDetails()
                .MinimumLevel.ControlledBy(loggingLevelSwitch)
                .WriteTo.Sentry(config =>
                {
                    var appSettings = SnykExtension.GetAppSettings();

                    config.Release = SnykExtension.GetIntegrationVersion();

                    config.Environment = appSettings.Environment;
                    config.Dsn = appSettings.SentryDsn;

                    config.AttachStacktrace = true;

                    sentryConfiguration = config;
                })
                .WriteTo.File(
                    Path.Combine(SnykDirectory.GetSnykAppDataDirectoryPath(), "snyk-extension.log"),
                    fileSizeLimitBytes: LogFileSize,
                    outputTemplate: OutputTemplate,
                    shared: true)
                .CreateLogger();

        private static ILogger ForContext(Type type) => Logger.Value.ForContext(type).ForContext("ShortSourceContext", type.Name);
    }
}
