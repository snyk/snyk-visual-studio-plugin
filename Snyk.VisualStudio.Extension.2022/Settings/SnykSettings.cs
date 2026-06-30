// ABOUTME: This file defines the data structure for persisted Snyk settings stored on disk
// ABOUTME: It contains global and solution-specific configuration including authentication tokens, scan preferences, and folder configs
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Contains Snyk extension settings.
    /// </summary>
    public class SnykSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykSettings"/> class.
        /// </summary>
        public SnykSettings()
        {
        }
        /// <summary>
        /// Gets or sets current Cli version.
        /// </summary>
        public string CurrentCliVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether snyk code security enabled.
        /// </summary>
        public bool SnykCodeSecurityEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether Secrets scanning is enabled.
        /// </summary>
        public bool SecretsEnabled { get; set; } = false;
        
        /// <summary>
        /// Gets or sets a value indicating whether oss enabled.
        /// </summary>
        public bool OssEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether binaries auto update is enabled.
        /// </summary>
        public bool BinariesAutoUpdateEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the value of the custom CLI path.
        /// </summary>
        public string CustomCliPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an array of workspace trusted folders.
        /// </summary>
        public ISet<string> TrustedFolders { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets Authentication Type.
        /// </summary>
        public AuthenticationType AuthenticationMethod { get; set; }

        public bool AutoScan { get; set; } = true;
        public string Token { get; set; } = string.Empty;
        public bool IacEnabled { get; set; } = true;
        public string CliReleaseChannel { get; set; } = SnykCliDownloader.DefaultReleaseChannel;
        public string CliBaseDownloadURL { get; set; } = SnykCliDownloader.DefaultBaseDownloadUrl;
        public bool IgnoreUnknownCa { get; set; }
        public string Organization { get; set; }
        public string CustomEndpoint { get; set; }
        public string DeviceId { get; set; } = Guid.NewGuid().ToString();
        public bool OpenIssuesEnabled { get; set; } = true;
        public bool IgnoredIssuesEnabled { get; set; } = false;
        public bool EnableDeltaFindings { get; set; }
        public bool AnalyticsPluginInstalledSent { get; set; }
        public bool FilterCritical { get; set; } = true;
        public bool FilterHigh { get; set; } = true;
        public bool FilterMedium { get; set; } = true;
        public bool FilterLow { get; set; } = true;
        public string AdditionalEnv { get; set; } = string.Empty;
        public List<string> AdditionalParameters { get; set; } = new List<string>();
        public int? RiskScoreThreshold { get; set; } = null;

        /// <summary>
        /// Legacy per-solution settings, keyed by <c>solutionFolder.ToLower().GetHashCode()</c>
        /// (the original key scheme). The live feature was removed in IDE-1651; this property is
        /// retained ONLY so an upgrading user's existing entries survive on disk until they are
        /// migrated into folder configs as each solution is opened (see
        /// <see cref="LegacySolutionSettingsMigrator"/> and
        /// <see cref="SnykOptionsManager.MigrateLegacySolutionSettings"/>). It is never written by
        /// feature code; the migrator removes entries as they are migrated and nulls it out once
        /// empty. Null when settings.json carries no legacy section, so it is not re-emitted
        /// (NullValueHandling.Ignore keeps the key out of fresh users' settings.json entirely).
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<int, LegacySolutionSettings> SolutionSettingsDict { get; set; }

        /// <summary>
        /// Set of pflag keys the user explicitly overrode from plugin defaults (IDE-2152).
        /// Null when absent from disk (first load / pre-upgrade file) — treated as empty by
        /// <see cref="SnykOptionsManager.Load"/>, which seeds from values in that case.
        /// NullValueHandling.Ignore keeps the key out of settings.json until at least one key
        /// is tracked, matching the behaviour of <see cref="SolutionSettingsDict"/>.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public HashSet<string> ChangedConfigKeys { get; set; }

        /// <summary>
        /// Persisted seeded-marker for the load-time seeding lifecycle (IDE-2152).
        /// Absent (false) on pre-feature / fresh-install settings files. <see cref="SnykOptionsManager.Load"/>
        /// checks this flag to determine whether the override set has ever been seeded:
        /// <list type="bullet">
        ///   <item>Absent/false + <see cref="ChangedConfigKeys"/> null/empty → seed once from value-vs-default,
        ///   then persist both the resulting set and this marker to disk.</item>
        ///   <item>Absent/false + <see cref="ChangedConfigKeys"/> non-empty → keys were written by a prior
        ///   version without the marker; hydrate verbatim and persist the marker.</item>
        ///   <item>True → the persisted set is the authoritative source of truth; hydrate verbatim
        ///   (including an empty set = "seeded, zero user overrides").</item>
        /// </list>
        /// <see cref="DefaultValueHandling.Ignore"/> keeps the key out of settings.json when false
        /// (matching "absent on pre-feature files"). IDE-only: never added to
        /// <see cref="ChangedConfigKeys"/> and never sent over the language-server wire.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ChangedConfigKeysSeeded { get; set; }
    }
}
