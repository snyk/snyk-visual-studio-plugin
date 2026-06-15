// ABOUTME: Pure mapping/merge logic that migrates a legacy per-solution settings entry into a FolderConfig.
// ABOUTME: One-way (path -> hash) lookup; never clobbers values the Language Server already supplied.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Converts a legacy <see cref="LegacySolutionSettings"/> entry into the folder config model and
    /// merges it into an existing folder-config list. Stateless and side-effect free so it can be
    /// unit-tested in isolation; <see cref="SnykOptionsManager.MigrateLegacySolutionSettings"/> drives
    /// the lookup, persistence, and entry removal.
    /// </summary>
    public static class LegacySolutionSettingsMigrator
    {
        /// <summary>
        /// Computes the legacy dictionary key for a solution folder. This MUST reproduce the original
        /// <c>SnykOptionsManager.GetSolutionPathHashAsync</c> key exactly —
        /// <c>(await GetSolutionFolderAsync()).ToLower().GetHashCode()</c> — so a path → hash lookup
        /// resolves the same entry the old code wrote. The one-way hash is not a problem: migration
        /// only ever goes path → hash, and the open solution folder is known at migration time.
        /// <para>
        /// <c>ToLower()</c> (culture-sensitive, not invariant) is intentional: it matches the original
        /// key byte-for-byte. <c>String.GetHashCode()</c> is stable within a given runtime/architecture
        /// — the same condition under which the legacy feature read its data back across sessions — so
        /// a lookup hits for any user who would have had working per-solution settings. If a user moves
        /// to a different bitness/runtime the hash differs and the lookup simply misses (no migration,
        /// defaults apply), which is acceptable.
        /// </para>
        /// </summary>
        public static int ComputeFolderHash(string folderPath) => folderPath.ToLower().GetHashCode();

        /// <summary>
        /// Maps a legacy entry to a <see cref="FolderConfig"/> for the given folder path. Only the
        /// fields with a clean target are migrated (additional parameters / environment / org);
        /// <c>IsAllProjectsScanEnabled</c> is intentionally dropped (dead since scanning moved to the
        /// Language Server).
        /// </summary>
        public static FolderConfig ToFolderConfig(LegacySolutionSettings legacy, string folderPath)
        {
            var fc = new FolderConfig { FolderPath = folderPath };

            var args = SplitArguments(legacy.AdditionalOptions);
            if (args.Count > 0)
                fc.AdditionalParameters = args;

            if (!string.IsNullOrEmpty(legacy.AdditionalEnv))
                fc.AdditionalEnv = legacy.AdditionalEnv;

            // Prefer the newer per-folder PreferredOrg; fall back to the pre-split single Organization
            // field, which was itself a user-set per-solution override, so older profiles still migrate.
            var preferredOrg = !string.IsNullOrEmpty(legacy.PreferredOrg) ? legacy.PreferredOrg : legacy.Organization;
            if (!string.IsNullOrEmpty(preferredOrg))
                fc.PreferredOrg = preferredOrg;
            if (!string.IsNullOrEmpty(legacy.AutoDeterminedOrg))
                fc.AutoDeterminedOrg = legacy.AutoDeterminedOrg;
            // Treat the org as user-set if the legacy flag said so, or if we recovered one from the
            // legacy Organization field (which only existed because the user set it).
            fc.OrgSetByUser = legacy.OrgSetByUser || !string.IsNullOrEmpty(preferredOrg);

            return fc;
        }

        /// <summary>
        /// Merges <paramref name="migrated"/> into a copy of <paramref name="existing"/>. If no entry
        /// for the folder exists it is appended; otherwise only empty fields are filled, so a value the
        /// Language Server already provided for that folder is never overwritten.
        /// </summary>
        public static List<FolderConfig> Merge(List<FolderConfig> existing, FolderConfig migrated)
        {
            var result = existing != null ? new List<FolderConfig>(existing) : new List<FolderConfig>();

            var match = result.FirstOrDefault(fc => fc != null &&
                string.Equals(fc.FolderPath, migrated.FolderPath, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                result.Add(migrated);
                return result;
            }

            if ((match.AdditionalParameters == null || match.AdditionalParameters.Count == 0)
                && migrated.AdditionalParameters != null)
                match.AdditionalParameters = migrated.AdditionalParameters;

            if (string.IsNullOrEmpty(match.AdditionalEnv) && !string.IsNullOrEmpty(migrated.AdditionalEnv))
                match.AdditionalEnv = migrated.AdditionalEnv;

            if (string.IsNullOrEmpty(match.PreferredOrg) && !string.IsNullOrEmpty(migrated.PreferredOrg))
            {
                match.PreferredOrg = migrated.PreferredOrg;
                match.OrgSetByUser = migrated.OrgSetByUser;
            }

            if (string.IsNullOrEmpty(match.AutoDeterminedOrg) && !string.IsNullOrEmpty(migrated.AutoDeterminedOrg))
                match.AutoDeterminedOrg = migrated.AutoDeterminedOrg;

            return result;
        }

        /// <summary>
        /// Splits a raw additional-CLI-parameters string into individual arguments, respecting
        /// double-quoted segments (so <c>--org "my org"</c> → <c>["--org", "my org"]</c>).
        /// </summary>
        public static List<string> SplitArguments(string raw)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(raw))
                return result;

            var sb = new StringBuilder();
            var inQuotes = false;
            foreach (var ch in raw)
            {
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && char.IsWhiteSpace(ch))
                {
                    if (sb.Length > 0)
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }

                    continue;
                }

                sb.Append(ch);
            }

            if (sb.Length > 0)
                result.Add(sb.ToString());

            return result;
        }
    }
}
