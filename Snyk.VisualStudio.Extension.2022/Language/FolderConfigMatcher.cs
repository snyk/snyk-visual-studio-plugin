// ABOUTME: Shared rule for deciding whether a folder config belongs to the current VS solution.
// ABOUTME: Single source of truth for both the LS->IDE path and the settings save path.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Snyk.VisualStudio.Extension.Language
{
    /// <summary>
    /// Matching rules for deciding whether a folder config belongs to the current Visual Studio
    /// solution. VS is single-solution (one root folder — see
    /// <see cref="Service.ISolutionService.GetSolutionFolderAsync"/>), but LS-supplied folder
    /// paths and the path returned by <c>GetSolutionFolderAsync</c> don't always normalise
    /// identically, so matching is case-insensitive and tolerant of trailing-separator /
    /// subfolder differences. Centralised here so the LS->IDE path
    /// (<see cref="SnykLanguageClientCustomTarget"/>) and the settings save path
    /// (<see cref="UI.Html.HtmlSettingsScriptingBridge"/>) can never drift apart.
    /// </summary>
    public static class FolderConfigMatcher
    {
        /// <summary>
        /// True when <paramref name="folderPath"/> identifies the same folder as
        /// <paramref name="currentSolutionPath"/> — either an exact (case-insensitive) match, or
        /// the solution path sits within the config path (defensive handling of path-normalisation
        /// differences between the Language Server and the IDE's solution service). Empty paths
        /// never match.
        /// </summary>
        public static bool Matches(string folderPath, string currentSolutionPath)
        {
            if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(currentSolutionPath))
                return false;

            // Unify separator style ('/' vs '\' — the LS may emit forward slashes while the IDE
            // uses backslashes) and trailing slashes on both sides. Comparison stays
            // case-insensitive, which already absorbs the casing differences common on Windows
            // (drive letter, UNC server name). We deliberately avoid Path.GetFullPath here: it can
            // throw on malformed input, resolves against the process working directory, and still
            // wouldn't reconcile 8.3/symlink forms — and these are already-absolute folder paths
            // from the IDE / Language Server, not relative user input.
            var config = Normalize(folderPath);
            var solution = Normalize(currentSolutionPath);

            if (config.Equals(solution, StringComparison.OrdinalIgnoreCase))
                return true;

            // In normal operation paths match exactly; this fallback handles the edge of the
            // solution living in a subfolder of the configured path.
            return solution.StartsWith(config + "\\", StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string path) => path.Replace('/', '\\').TrimEnd('\\');

        /// <summary>
        /// Returns the first folder config whose <see cref="FolderConfig.FolderPath"/>
        /// <see cref="Matches"/> the current solution path, or null if none do.
        /// </summary>
        public static FolderConfig FindFirstMatching(IEnumerable<FolderConfig> folderConfigs, string currentSolutionPath)
        {
            if (folderConfigs == null || string.IsNullOrEmpty(currentSolutionPath))
                return null;

            return folderConfigs.FirstOrDefault(fc => fc != null && Matches(fc.FolderPath, currentSolutionPath));
        }
    }
}
