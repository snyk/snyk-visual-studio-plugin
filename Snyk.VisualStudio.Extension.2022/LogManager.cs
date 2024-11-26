using System;
using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;

namespace Snyk.VisualStudio.Extension
{
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

        private static Lazy<Logger> Logger { get; } = new Lazy<Logger>(CreateLogger);

        /// <summary>
        /// Create Logger instance for provided type.
        /// </summary>
        /// <typeparam name="T">Type of class.</typeparam>
        /// <returns><see cref="ILogger"/> implementation for class.</returns>
        public static ILogger ForContext<T>() => ForContext(typeof(T));

        private static Logger CreateLogger() => new LoggerConfiguration()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithExceptionDetails()
                .MinimumLevel.ControlledBy(loggingLevelSwitch)
                .WriteTo.File(
                    Path.Combine(SnykDirectory.GetSnykAppDataDirectoryPath(), "snyk-extension.log"),
                    fileSizeLimitBytes: LogFileSize,
                    outputTemplate: OutputTemplate,
                    shared: true)
                .CreateLogger();

        private static ILogger ForContext(Type type) => Logger.Value.ForContext(type).ForContext("ShortSourceContext", type.Name);
    }
}
