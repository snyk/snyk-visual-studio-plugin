using System;
using System.Diagnostics;
using System.IO;
using Serilog;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// The path and URI rules of <see cref="WebView2Host"/>: where a host's Chromium user-data
    /// folder lives, when a stale one may be swept, and which documents the control is allowed to
    /// navigate to. None of this needs the WebView2 control or WPF, so it is separated from the
    /// host itself and covered by the cross-platform test suite.
    /// </summary>
    public sealed partial class WebView2Host
    {
        private static readonly ILogger Logger = LogManager.ForContext<WebView2Host>();

        /// <summary>
        /// Builds a per-context + per-VS-process user-data folder path under
        /// <c>%LOCALAPPDATA%\Snyk\WebView2\&lt;pid&gt;\&lt;contextKey&gt;</c>. Production
        /// uses two keys: <c>"toolwindow"</c> (shared by the description and summary
        /// panels) and <c>"settings"</c> (the modal dialog, see class-level remarks for
        /// why it's isolated). The per-process root is essential because WebView2 takes
        /// an exclusive lock on the user-data folder — two VS instances running
        /// concurrently would otherwise contend. Sibling <c>&lt;pid&gt;</c> folders whose
        /// process has exited are swept on first call so they don't accumulate across
        /// crashed sessions.
        /// </summary>
        public static string BuildUserDataFolder(string contextKey)
        {
            if (string.IsNullOrEmpty(contextKey)) throw new ArgumentException("Context key is required", nameof(contextKey));

            _ = OrphanCleanupOnce.Value;

            var pid = Process.GetCurrentProcess().Id;
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Snyk", "WebView2", pid.ToString(), contextKey);
        }

        private static readonly Lazy<bool> OrphanCleanupOnce = new Lazy<bool>(() =>
        {
            TryCleanupOrphanFolders();
            return true;
        });

        private static void TryCleanupOrphanFolders()
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Snyk", "WebView2");

            if (!Directory.Exists(root)) return;

            var currentPid = Process.GetCurrentProcess().Id;
            foreach (var dir in Directory.GetDirectories(root))
            {
                var name = Path.GetFileName(dir);
                if (!int.TryParse(name, out var pid)) continue;
                if (pid == currentPid) continue;
                if (IsProcessAlive(pid)) continue;

                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Failed to clean up orphan WebView2 folder {Path}", dir);
                }
            }
        }

        private static bool IsProcessAlive(int pid)
        {
            try
            {
                using (var process = Process.GetProcessById(pid))
                {
                    return !process.HasExited;
                }
            }
            catch (ArgumentException)
            {
                // GetProcessById throws when the PID does not refer to a running process.
                return false;
            }
            catch (InvalidOperationException)
            {
                // Process has exited between GetProcessById and the HasExited check.
                return false;
            }
        }

        // internal static for testability (InternalsVisibleTo test project): pure allowlist logic
        // with the user-data folder passed in rather than read from instance state.
        internal static bool IsAllowedDocumentUri(string uri, string userDataFolder)
        {
            if (string.IsNullOrEmpty(uri))
                return true; // initial / empty document

            // NavigateToString surfaces as about:blank or a data: document depending on size.
            if (uri.StartsWith("about:", StringComparison.OrdinalIgnoreCase)
                || uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return true;

            // Oversized HTML is spilled to a scratch file and loaded via file://; only allow files
            // under this host's own user-data folder.
            if (uri.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var path = Path.GetFullPath(new Uri(uri).LocalPath);
                    var root = Path.GetFullPath(userDataFolder).TrimEnd(
                        Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    return path.StartsWith(root, StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}
