// ABOUTME: Maps each global pflag key to its plugin default value, mirroring the SnykSettings field
// ABOUTME: initializers. Used by UserOverrideTracker to seed the override set on first load (IDE-2152).
using System.Collections.Generic;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Download;

namespace Snyk.VisualStudio.Extension.Language
{
    // Default values for each global pflag key, derived from SnykSettings field initializers.
    // Keys absent from this map are treated as "no default known" — IsDefault returns false.
    internal static class ConfigDefaults
    {
        // Mirrors SnykSettings field initializers exactly so seed comparison is accurate.
        // CLI string defaults reference SnykCliDownloader constants (same source as SnykSettings
        // initializers) so they cannot silently drift. Boolean product/scan/severity/view defaults
        // are set to the same literal values as SnykSettings field initializers; the test
        // ConfigDefaults_BooleanValues_MatchSnykSettingsFieldInitializers (ConfigDefaultsTests.cs)
        // acts as a drift guard by constructing new SnykSettings() and comparing via GetDefaultForTest.
        private static readonly Dictionary<string, object> Defaults = new Dictionary<string, object>
        {
            // Products
            [PflagKeys.SnykOssEnabled]           = true,
            [PflagKeys.SnykCodeEnabled]           = true,
            [PflagKeys.SnykIacEnabled]            = true,
            [PflagKeys.SnykSecretsEnabled]        = false,

            // Scan
            [PflagKeys.ScanAutomatic]             = true,
            [PflagKeys.ScanNetNew]                = false,

            // Severity filters
            [PflagKeys.SeverityFilterCritical]    = true,
            [PflagKeys.SeverityFilterHigh]        = true,
            [PflagKeys.SeverityFilterMedium]      = true,
            [PflagKeys.SeverityFilterLow]         = true,

            // Issue view
            [PflagKeys.IssueViewOpenIssues]       = true,
            [PflagKeys.IssueViewIgnoredIssues]    = false,

            // Connection / auth
            [PflagKeys.ApiEndpoint]               = string.Empty,
            [PflagKeys.Token]                     = string.Empty,
            // AuthenticationMethod default is the zero value of the enum (OAuth).
            // Matches SnykSettings.AuthenticationMethod which has no explicit initializer.
            [PflagKeys.AuthenticationMethod]      = default(AuthenticationType).ToString().ToLowerInvariant(),
            [PflagKeys.Organization]              = string.Empty,
            [PflagKeys.ProxyInsecure]             = false,

            // CLI / binary — reference canonical SnykCliDownloader constants so this map
            // cannot drift from the SnykSettings field initializers that use the same constants.
            [PflagKeys.AutomaticDownload]         = true,
            [PflagKeys.CliPath]                   = string.Empty,
            [PflagKeys.CliReleaseChannel]         = SnykCliDownloader.DefaultReleaseChannel,
            [PflagKeys.BinaryBaseUrl]             = SnykCliDownloader.DefaultBaseDownloadUrl,

            // Trust — TrustedFolders is not yielded by GetGlobalKeyValues (it is folder-scoped, not
            // global). This entry is defensive: if IsDefault is ever called for this key it returns
            // the correct answer rather than "unknown key → not default".
            [PflagKeys.TrustedFolders]            = new System.Collections.Generic.HashSet<string>(),

            // Env / params (global defaults are empty)
            [PflagKeys.AdditionalEnvironment]     = string.Empty,
            [PflagKeys.AdditionalParameters]      = string.Empty,

            // Risk score: null means "not set" == default.
            // Stored explicitly so IsDefault(key, null) works via the defaultValue==null branch.
            [PflagKeys.RiskScoreThreshold]        = null,
        };

        /// <summary>
        /// Returns true when <paramref name="value"/> equals the plugin default for
        /// <paramref name="key"/>. Returns false for unknown keys (conservative: treat as overridden).
        /// </summary>
        public static bool IsDefault(string key, object value)
        {
            if (!Defaults.TryGetValue(key, out var defaultValue))
                return false;

            if (defaultValue == null)
                return value == null;

            // For collection types, two empty collections are considered equal to the default.
            // This is correct only because all collection defaults in Defaults are currently empty;
            // if a future key has a non-empty collection default this branch must be replaced with
            // a proper set-equality comparison.
            if (defaultValue is System.Collections.ICollection defaultCol)
            {
                if (value is System.Collections.ICollection valCol)
                    return valCol.Count == 0 && defaultCol.Count == 0;
                return false;
            }

            // AdditionalParameters: the form sends a space-joined string from a text input.
            // An empty or whitespace-only string ("   ") is semantically equivalent to the empty
            // default (no parameters) — splitting on spaces produces an empty token list either way.
            if (key == PflagKeys.AdditionalParameters && value is string paramStr)
                return string.IsNullOrWhiteSpace(paramStr);

            return defaultValue.Equals(value);
        }

        /// <summary>
        /// Test helper: returns the raw default value for <paramref name="key"/> from the
        /// <see cref="Defaults"/> map, or null when the key is absent. Used by drift-guard tests
        /// to compare ConfigDefaults entries against <see cref="Settings.SnykSettings"/> field
        /// initializers without exposing the dictionary publicly.
        /// </summary>
        internal static object GetDefaultForTest(string key)
        {
            Defaults.TryGetValue(key, out var value);
            return value;
        }
    }
}
