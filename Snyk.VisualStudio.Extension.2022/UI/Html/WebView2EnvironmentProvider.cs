using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// Owns the single <see cref="CoreWebView2Environment"/> shared by every WebView2 control
    /// the plugin creates in this Visual Studio process. WebView2 takes an exclusive lock on
    /// the user-data folder, so per-control environments would lock out a second VS instance —
    /// per-process sharing eliminates that risk and reduces overhead (one Edge browser process
    /// for the plugin instead of one per panel).
    /// </summary>
    /// <remarks>
    /// Folder layout: <c>%LOCALAPPDATA%\Snyk\WebView2\&lt;pid&gt;\</c> as the per-VS-process root,
    /// containing <c>profile\</c> (the Chromium user-data folder) and <c>scratch\&lt;panel&gt;\</c>
    /// (oversized-HTML spill files; see <see cref="WebView2NavigationPreparer"/>).
    /// On first use we sweep any sibling <c>&lt;pid&gt;\</c> folders whose process is no longer
    /// alive, preventing accumulation across crashed VS sessions.
    /// </remarks>
    internal static class WebView2EnvironmentProvider
    {
        private static readonly ILogger Logger = LogManager.ForContext(typeof(WebView2EnvironmentProvider));
        private static readonly object Gate = new object();
        private static Task<CoreWebView2Environment> _environmentTask;

        public static Task<CoreWebView2Environment> GetAsync()
        {
            lock (Gate)
            {
                if (_environmentTask == null)
                {
                    _environmentTask = CreateEnvironmentAsync();
                }
                return _environmentTask;
            }
        }

        public static string GetScratchDirectory(string panelKey)
        {
            if (string.IsNullOrEmpty(panelKey)) throw new ArgumentException("Panel key is required", nameof(panelKey));
            return Path.Combine(GetProcessRoot(), "scratch", panelKey);
        }

        internal static string GetProcessRoot()
        {
            var pid = Process.GetCurrentProcess().Id;
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Snyk", "WebView2", pid.ToString());
        }

        private static async Task<CoreWebView2Environment> CreateEnvironmentAsync()
        {
            TryCleanupOrphanFolders();
            var profileDir = Path.Combine(GetProcessRoot(), "profile");
            return await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: profileDir,
                options: null);
        }

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
    }
}
