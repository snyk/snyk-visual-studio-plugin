using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Language
{
    // Converts ISnykOptions to the v25 pflag-keyed wire shape.
    public class LsSettingsV25
    {
        private readonly ISnykServiceProvider serviceProvider;

        public LsSettingsV25(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public InitializationOptionsV25 GetInitializationOptions()
        {
            if (serviceProvider == null)
                return null;

            var options = serviceProvider.Options;
            // BuildSettingsMap consumes pending resets destructively (ConsumePendingResets clears the
            // queue). The result MUST be sent to the LS immediately — do not call BuildSettingsMap
            // for inspection only, as doing so would silently discard queued reset signals.
            return new InitializationOptionsV25
            {
                Settings = BuildSettingsMap(options),
                FolderConfigs = BuildFolderConfigs(options.FolderConfigs),
                RequiredProtocolVersion = "25",
                DeviceId = options.DeviceId,
                IntegrationName = GetIntegrationName(options),
                IntegrationVersion = GetIntegrationVersion(options),
                OsPlatform = GetOsPlatform(),
                OsArch = GetOsArch(),
                RuntimeName = ".NET Framework",
                RuntimeVersion = Environment.Version.ToString(),
                HoverVerbosity = 1,
                OutputFormat = "plain",
            };
        }

        // internal for testability (InternalsVisibleTo test project).
        // Note: additional_parameters and additional_environment are sent both in the global
        // settings map (Project Defaults tab) and per-folder in folderConfigs. The LS resolves
        // folder-over-global. Global values are wired via ISnykOptions.AdditionalParameters /
        // AdditionalEnv; per-folder values live in FolderConfig.
        internal Dictionary<string, ConfigSetting> BuildSettingsMap(ISnykOptions options)
        {
            // Obtain the tracker from the options manager. When null (e.g. in tests that do not wire
            // the manager), fall back to changed:true so every key is treated as user-overridden
            // (safe: matches the pre-IDE-2152 behaviour).
            var tracker = serviceProvider?.SnykOptionsManager?.OverrideTracker;

            // Helper: sets the changed flag from the tracker for this key.
            // Gate on IsSeeded: an unseeded tracker (Load() has not run yet) falls back to
            // changed:true so a startup-ordering race cannot silently downgrade user overrides by
            // sending changed:false before persistence has been read. Once seeded, the tracker
            // distinguishes overridden keys (changed:true) from untouched ones (changed:false).
            var trackerSeeded = tracker != null && tracker.IsSeeded;
            ConfigSetting Cs(string key, object value) =>
                ConfigSetting.Of(value, trackerSeeded ? tracker.IsChanged(key) : true);

            var map = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.SnykOssEnabled]          = Cs(PflagKeys.SnykOssEnabled,         options.OssEnabled),
                [PflagKeys.SnykCodeEnabled]         = Cs(PflagKeys.SnykCodeEnabled,        options.SnykCodeSecurityEnabled),
                [PflagKeys.SnykIacEnabled]          = Cs(PflagKeys.SnykIacEnabled,         options.IacEnabled),
                [PflagKeys.SnykSecretsEnabled]      = Cs(PflagKeys.SnykSecretsEnabled,     options.SecretsEnabled),

                // Send the persisted user preference (AutoScan), not the InternalAutoScan runtime
                // gate. The gate only delays the *first* scan until the IDE is ready (handled by the
                // scan-trigger logic in OnSnykConfiguration / OnHasAuthenticated) — it must not be the
                // value we tell the LS to persist, or a manual-mode choice gets overwritten by the
                // gate's post-first-scan `true` on the next config round-trip.
                [PflagKeys.ScanAutomatic]           = Cs(PflagKeys.ScanAutomatic,          options.AutoScan),
                [PflagKeys.ScanNetNew]              = Cs(PflagKeys.ScanNetNew,             options.EnableDeltaFindings),

                [PflagKeys.SeverityFilterCritical]  = Cs(PflagKeys.SeverityFilterCritical, options.FilterCritical),
                [PflagKeys.SeverityFilterHigh]      = Cs(PflagKeys.SeverityFilterHigh,     options.FilterHigh),
                [PflagKeys.SeverityFilterMedium]    = Cs(PflagKeys.SeverityFilterMedium,   options.FilterMedium),
                [PflagKeys.SeverityFilterLow]       = Cs(PflagKeys.SeverityFilterLow,      options.FilterLow),

                [PflagKeys.IssueViewOpenIssues]     = Cs(PflagKeys.IssueViewOpenIssues,    options.OpenIssuesEnabled),
                [PflagKeys.IssueViewIgnoredIssues]  = Cs(PflagKeys.IssueViewIgnoredIssues, options.IgnoredIssuesEnabled),

                [PflagKeys.ApiEndpoint]             = Cs(PflagKeys.ApiEndpoint,            options.CustomEndpoint ?? string.Empty),
                [PflagKeys.Token]                   = Cs(PflagKeys.Token,                  options.ApiToken?.ToString() ?? string.Empty),
                [PflagKeys.Organization]            = Cs(PflagKeys.Organization,           options.Organization ?? string.Empty),
                [PflagKeys.AuthenticationMethod]    = Cs(PflagKeys.AuthenticationMethod,   options.AuthenticationMethod.ToString().ToLowerInvariant()),
                [PflagKeys.ProxyInsecure]           = Cs(PflagKeys.ProxyInsecure,          options.IgnoreUnknownCA),

                [PflagKeys.AutomaticDownload]       = Cs(PflagKeys.AutomaticDownload,      options.BinariesAutoUpdate),
                [PflagKeys.CliPath]                 = Cs(PflagKeys.CliPath,                SnykCli.GetCliFilePath(options.CliCustomPath)),
                [PflagKeys.BinaryBaseUrl]           = Cs(PflagKeys.BinaryBaseUrl,
                                                         options.CliBaseDownloadURL ?? SnykCliDownloader.DefaultBaseDownloadUrl),
                [PflagKeys.CliReleaseChannel]       = Cs(PflagKeys.CliReleaseChannel,
                                                         options.CliReleaseChannel ?? SnykCliDownloader.DefaultReleaseChannel),

                // TrustedFolders is in AlwaysChanged so IsChanged returns true regardless.
                [PflagKeys.TrustedFolders]          = Cs(PflagKeys.TrustedFolders,         options.TrustedFolders?.ToList() ?? new List<string>()),
                [PflagKeys.AdditionalEnvironment]   = Cs(PflagKeys.AdditionalEnvironment,  options.AdditionalEnv ?? string.Empty),
                // LS applyCliConfig reads additional_parameters via settingStr (string type-assert),
                // so send as a space-joined string — same wire format LS uses on its outbound echo.
                [PflagKeys.AdditionalParameters]    = Cs(PflagKeys.AdditionalParameters,
                                                         string.Join(" ", options.AdditionalParameters ?? new List<string>())),

                // DeviceId and ClientProtocolVersion are not user-settable; send as always-changed
                // to preserve backward compatibility (LS needs them on every handshake).
                [PflagKeys.DeviceId]                = ConfigSetting.Of(options.DeviceId ?? string.Empty),
                [PflagKeys.ClientProtocolVersion]   = ConfigSetting.Of("25"),
            };

            if (options.RiskScoreThreshold.HasValue)
                map[PflagKeys.RiskScoreThreshold] = Cs(PflagKeys.RiskScoreThreshold,
                    options.RiskScoreThreshold.Value);

            // Emit reset signals for keys that were just un-marked (returned to default).
            // These override the regular value entry for that key with {value:null, changed:true}.
            //
            // ConsumePendingResets() is destructive (clears the set). This is safe because
            // BuildSettingsMap is only ever called immediately before sending to the LS:
            //   • GetInitializationOptions()   → passed directly to SnykLanguageClient.InitializationOptions
            //   • GetLspConfigurationParam()   → passed directly to DidChangeConfigurationAsync
            // There is no build-without-send path, so resets cannot be silently discarded.
            //
            // Guard on trackerSeeded: an unseeded tracker has no meaningful pending resets
            // (it has never been hydrated from persistence, so nothing could have been un-marked).
            if (trackerSeeded)
            {
                foreach (var resetKey in tracker.ConsumePendingResets())
                    map[resetKey] = ConfigSetting.Reset();
            }

            return map;
        }

        private List<LspFolderConfig> BuildFolderConfigs(List<FolderConfig> folderConfigs)
        {
            if (folderConfigs == null || folderConfigs.Count == 0)
                return new List<LspFolderConfig>();

            var result = new List<LspFolderConfig>();
            foreach (var fc in folderConfigs)
            {
                if (fc == null) continue;

                // Round-trip the opaque settings map verbatim — the LS is authoritative over
                // folder-scoped settings, so the IDE forwards every key it was sent (and every key
                // it set IDE-side, incl. resets stored as {value:null, changed:true}) without
                // cherry-picking. Copy so callers don't mutate the stored map.
                var settings = fc.Settings != null
                    ? new Dictionary<string, ConfigSetting>(fc.Settings, StringComparer.Ordinal)
                    : new Dictionary<string, ConfigSetting>();

                result.Add(new LspFolderConfig
                {
                    FolderPath = fc.FolderPath,
                    Settings = settings,
                });
            }
            return result;
        }

        public LspConfigurationParam GetLspConfigurationParam()
        {
            if (serviceProvider == null)
                return null;

            var options = serviceProvider.Options;
            // BuildSettingsMap consumes pending resets destructively (ConsumePendingResets clears the
            // queue). The result MUST be sent to the LS immediately — do not call BuildSettingsMap
            // for inspection only, as doing so would silently discard queued reset signals.
            return new LspConfigurationParam
            {
                Settings = BuildSettingsMap(options),
                FolderConfigs = BuildFolderConfigs(options.FolderConfigs),
            };
        }

        private string GetIntegrationName(ISnykOptions options) =>
            $"{options.IntegrationEnvironment}@@{options.IntegrationName}";

        private string GetIntegrationVersion(ISnykOptions options) =>
            $"{options.IntegrationEnvironmentVersion}@@{options.IntegrationVersion}";

        private static string GetOsPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "windows";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "darwin";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
            return "unknown";
        }

        private static string GetOsArch()
        {
            switch (RuntimeInformation.OSArchitecture)
            {
                case Architecture.X64: return "amd64";
                case Architecture.Arm64: return "arm64";
                case Architecture.X86: return "386";
                default: return RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();
            }
        }
    }
}
