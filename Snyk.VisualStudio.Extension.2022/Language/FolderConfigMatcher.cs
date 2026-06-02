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

            // Trim trailing separators on both sides so a config path that differs only by a
            // trailing slash still counts as the same folder.
            var config = folderPath.TrimEnd('\\', '/');
            var solution = currentSolutionPath.TrimEnd('\\', '/');

            if (config.Equals(solution, StringComparison.OrdinalIgnoreCase))
                return true;

            // In normal operation paths match exactly; this fallback handles the edge case the
            // LS->IDE path has long guarded against — the solution living in a subfolder of the
            // configured path (LS vs IDE path-normalisation differences).
            return solution.StartsWith(config + "\\", StringComparison.OrdinalIgnoreCase)
                || solution.StartsWith(config + "/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns the first folder config whose <see cref="FolderConfig.FolderPath"/>
        /// <see cref="Matches"/> the current solution path, or null if none do.
        /// </summary>
        public static FolderConfig FindMatching(IEnumerable<FolderConfig> folderConfigs, string currentSolutionPath)
        {
            if (folderConfigs == null || string.IsNullOrEmpty(currentSolutionPath))
                return null;

            return folderConfigs.FirstOrDefault(fc => fc != null && Matches(fc.FolderPath, currentSolutionPath));
        }
    }
}
