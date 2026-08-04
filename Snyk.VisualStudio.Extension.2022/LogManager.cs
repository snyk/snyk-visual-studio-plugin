using System;
using System.IO;
using Microsoft.VisualStudio.Shell;
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
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{ProcessId:00000}] {Level:u4} [{ThreadId:00}] {ThreadContext,-15} {ShortSourceContext,-25} {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// 10 Mb.
        /// </summary>
        private const int LogFileSize = 10485760;

        private static LoggingLevelSwitch loggingLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

        // FIX-D2 (IDE-1483): Use the default Lazy<T> ExecutionAndPublication mode (single-init).
        // CreateLogger() is wrapped in try/catch and falls back to CreateFallbackLogger() on any
        // failure, so it can never throw — which means the Lazy will never cache an exception
        // (there is nothing to cache).  Single-init is therefore safe AND preferred: it guarantees
        // only ONE thread ever calls CreateLogger(), preventing the concurrent double-open handle
        // leak that LazyThreadSafetyMode.PublicationOnly would allow (two threads both entering
        // CreateLogger() concurrently → two File.Open calls → the losing Logger's file handle
        // leaks until GC).
        private static Lazy<Logger> Logger { get; } = new Lazy<Logger>(CreateLogger);

        /// <summary>
        /// Create Logger instance for provided type.
        /// </summary>
        /// <typeparam name="T">Type of class.</typeparam>
        /// <returns><see cref="ILogger"/> implementation for class.</returns>
        public static ILogger ForContext<T>() => ForContext(typeof(T));

        private static Logger CreateLogger()
        {
            try
            {
                return new LoggerConfiguration()
                    .Enrich.WithProcessId()
                    .Enrich.WithThreadId()
                    .Enrich.WithExceptionDetails()
                    .Enrich.With<ThreadContextEnricher>() // Dynamically determine the thread context
                    .MinimumLevel.ControlledBy(loggingLevelSwitch)
                    .WriteTo.File(
                        Path.Combine(SnykDirectory.GetSnykAppDataDirectoryPath(), "snyk-extension.log"),
                        fileSizeLimitBytes: LogFileSize,
                        rollOnFileSizeLimit: true,
                        outputTemplate: OutputTemplate,
                        shared: true)
                    .CreateLogger();
            }
            catch (Exception)
            {
                // File sink creation failed (e.g. locked-down or UNC-redirected %LocalAppData%).
                // Fall back to a no-sink logger so logging calls inside catch blocks never re-throw
                // and crash the extension.  We cannot log the failure here (no logger yet), but
                // the degraded logger is fully usable for all subsequent ForContext() calls.
                return CreateFallbackLogger();
            }
        }

        /// <summary>
        /// Creates a minimal logger with no file sink used when the primary file sink is unavailable.
        /// Internal for testability.
        /// </summary>
        internal static Logger CreateFallbackLogger() =>
            new LoggerConfiguration()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .MinimumLevel.ControlledBy(loggingLevelSwitch)
                .CreateLogger();

        public static ILogger ForContext(Type type) => Logger.Value.ForContext(type)
            .ForContext("ShortSourceContext", type.Name);
    }

    /// <summary>
    /// Custom thread context enricher to dynamically determine if the thread is a UI thread or a background thread.
    /// </summary>
    public class ThreadContextEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var threadContext = ThreadHelper.CheckAccess() ? "UI Thread" : "Background Thread";
            var threadContextProperty = propertyFactory.CreateProperty("ThreadContext", threadContext);
            logEvent.AddPropertyIfAbsent(threadContextProperty);
        }
    }
}
