// ABOUTME: Unit tests for IDE-1483 FIX-D2 — LogManager.ForContext() must never throw even
// when the file sink cannot be created (locked-down or UNC-redirected %LocalAppData%).
//
// Before FIX-D2: CreateLogger() could throw, and the default Lazy<Logger> ExecutionAndPublication
// mode would cache that exception and re-throw it on every subsequent Logger.Value / ForContext()
// access, converting a logging-init failure into an unhandled exception that crashes startup.
//
// After FIX-D2: CreateLogger() is wrapped in try/catch and falls back to CreateFallbackLogger()
// on any file-sink failure, so it can never throw.  The Lazy therefore uses the default
// ExecutionAndPublication (single-init) mode — safe because there is never an exception to cache,
// and single-init avoids the concurrent double-open file-handle leak that PublicationOnly would
// allow.  ForContext()/ForContext<T>() therefore never throw.
//
// NOTE: These tests must be verified on Windows/CI — no .NET toolchain on the Linux
// build host used to author them.
using System;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    /// <summary>
    /// Tests for IDE-1483 FIX-D2: LogManager.ForContext() must return a usable ILogger and
    /// must not throw, even when the file sink cannot be created (locked %LocalAppData%,
    /// UNC-redirected paths, etc.).
    ///
    /// FIX-D2 wraps CreateLogger() in try/catch so it falls back to CreateFallbackLogger()
    /// on any file-sink failure and can therefore never throw.  The Lazy uses default
    /// ExecutionAndPublication (single-init) mode — safe because no exception is ever produced
    /// to be cached, and single-init avoids the concurrent double-open file-handle leak.
    ///
    /// We cannot force the real Lazy to use a bad path mid-process (already initialised), so
    /// we verify the contract indirectly: ForContext() on any Type must return non-null without
    /// throwing, and CreateFallbackLogger() (the internal helper exercised by the catch branch)
    /// must return a usable logger.
    /// </summary>
    public class LogManagerGracefulDegradationTests
    {
        // ----------------------------------------------------------------
        // D2-UNIT-001: ForContext(Type) does not throw and returns non-null
        // ----------------------------------------------------------------

        /// <summary>
        /// D2-UNIT-001: LogManager.ForContext(typeof(X)) must return a non-null ILogger
        /// and must not throw, regardless of file-sink availability.
        /// This test is GREEN after FIX-D2 and would be RED before it only in an
        /// environment where %LocalAppData%\Snyk\ cannot be written (simulated by the fix's
        /// fallback path — see production code comment for why a direct bad-path test is
        /// impractical in-process without reflection hacks).
        /// </summary>
        [Fact]
        public void ForContext_ReturnsNonNull_DoesNotThrow()
        {
            Serilog.ILogger result = null;
            var ex = Record.Exception(() => result = LogManager.ForContext(typeof(LogManagerGracefulDegradationTests)));

            Assert.Null(ex);
            Assert.NotNull(result);
        }

        // ----------------------------------------------------------------
        // D2-UNIT-002: ForContext<T>() does not throw and returns non-null
        // ----------------------------------------------------------------

        /// <summary>
        /// D2-UNIT-002: The generic overload LogManager.ForContext&lt;T&gt;() must return a
        /// non-null ILogger and must not throw.
        /// </summary>
        [Fact]
        public void ForContextGeneric_ReturnsNonNull_DoesNotThrow()
        {
            Serilog.ILogger result = null;
            var ex = Record.Exception(() => result = LogManager.ForContext<LogManagerGracefulDegradationTests>());

            Assert.Null(ex);
            Assert.NotNull(result);
        }

        // ----------------------------------------------------------------
        // D2-UNIT-003: FallbackLogger helper returns a usable logger for invalid path
        // ----------------------------------------------------------------

        /// <summary>
        /// D2-UNIT-003: LogManager.CreateFallbackLogger() (the internal helper introduced
        /// by FIX-D2) must return a non-null, usable Serilog.Core.Logger — this proves the
        /// degraded-logger path is correctly wired.
        ///
        /// This test exercises the EXACT branch taken when the primary file sink creation
        /// throws: CreateLogger() catches the exception and delegates to CreateFallbackLogger(),
        /// which builds a Serilog pipeline with no file sink (so it cannot fail and cannot
        /// throw).  Since CreateLogger() itself therefore never throws, the Lazy uses the
        /// default ExecutionAndPublication (single-init) mode safely.
        ///
        /// The test project is in InternalsVisibleTo("Snyk.VisualStudio.Extension.Tests")
        /// (AssemblyInfo.cs line 37), so the internal method is directly accessible.
        /// </summary>
        [Fact]
        public void CreateFallbackLogger_ReturnsUsableLogger_DoesNotThrow()
        {
            Serilog.Core.Logger fallback = null;
            var ex = Record.Exception(() => fallback = LogManager.CreateFallbackLogger());

            Assert.Null(ex);
            Assert.NotNull(fallback);

            // Must be usable — writing a log event must not throw.
            var writeEx = Record.Exception(() =>
                fallback.ForContext("Source", "test").Information("FIX-D2 fallback smoke test"));
            Assert.Null(writeEx);
        }
    }
}
