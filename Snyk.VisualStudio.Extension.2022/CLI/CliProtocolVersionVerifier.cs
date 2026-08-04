using System;
using System.Diagnostics;
using System.IO;
using Serilog;

namespace Snyk.VisualStudio.Extension.CLI
{
    /// <summary>
    /// Pre-launch gate (IDE-2404): stops the Language Server from ever being spawned against a CLI
    /// binary whose protocol version does not match what this plugin requires. Without this, a
    /// mismatch is only caught by snyk-ls's own post-handshake check (handleProtocolVersion), by
    /// which point the LS has already launched and the extension is stuck "loading" with no working
    /// settings UI. Mirrors IntelliJ's matchesRequiredLsProtocolVersion, VS Code's and Eclipse's
    /// verifyCliProtocolVersion.
    /// </summary>
    public static class CliProtocolVersionVerifier
    {
        // snyk-ls reports this instead of a numeric version for local/dev builds; treated as always
        // compatible, matching VS Code/Eclipse/IntelliJ.
        public const string DevelopmentProtocolVersion = "development";

        private static readonly ILogger Logger = LogManager.ForContext(typeof(CliProtocolVersionVerifier));
        private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Returns whether <paramref name="cliPath"/> reports a protocol version compatible with
        /// <paramref name="requiredProtocolVersion"/>.
        /// </summary>
        /// <param name="runCliProtocolVersionCheck">Injection seam for tests; defaults to actually
        /// spawning "&lt;cliPath&gt; language-server --protocolVersion".</param>
        public static bool IsCompatible(
            string cliPath,
            string requiredProtocolVersion,
            Func<string, string> runCliProtocolVersionCheck = null)
        {
            runCliProtocolVersionCheck ??= RunCliProtocolVersionCheck;

            if (string.IsNullOrEmpty(cliPath) || !File.Exists(cliPath))
            {
                Logger.Warning("Cannot verify CLI protocol version: '{CliPath}' does not exist.", cliPath);
                return false;
            }

            string reportedVersion;
            try
            {
                reportedVersion = runCliProtocolVersionCheck(cliPath)?.Trim();
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to determine CLI protocol version for {CliPath}.", cliPath);
                return false;
            }

            if (string.IsNullOrEmpty(reportedVersion))
            {
                Logger.Warning("CLI at {CliPath} did not report a protocol version.", cliPath);
                return false;
            }

            if (reportedVersion == DevelopmentProtocolVersion)
            {
                return true;
            }

            var isCompatible = reportedVersion == requiredProtocolVersion;
            if (!isCompatible)
            {
                Logger.Warning(
                    "CLI at {CliPath} reports protocol version {Actual}, required {Required}.",
                    cliPath, reportedVersion, requiredProtocolVersion);
            }

            return isCompatible;
        }

        private static string RunCliProtocolVersionCheck(string cliPath)
        {
            var info = new ProcessStartInfo
            {
                FileName = cliPath,
                Arguments = "language-server --protocolVersion",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = new Process { StartInfo = info })
            {
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                if (!process.WaitForExit((int)CheckTimeout.TotalMilliseconds))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                        // best effort
                    }

                    throw new TimeoutException(
                        $"CLI protocol version check timed out after {CheckTimeout.TotalSeconds}s for '{cliPath}'.");
                }

                return process.ExitCode == 0 ? output : null;
            }
        }
    }
}
