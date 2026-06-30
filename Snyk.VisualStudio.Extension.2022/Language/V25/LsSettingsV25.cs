using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Snyk.VisualStudio.Extension.CLI;
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
            var map = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.SnykOssEnabled] = ConfigSetting.Of(options.OssEnabled),
                [PflagKeys.SnykCodeEnabled] = ConfigSetting.Of(options.SnykCodeSecurityEnabled),
                [PflagKeys.SnykIacEnabled] = ConfigSetting.Of(options.IacEnabled),
                [PflagKeys.SnykSecretsEnabled] = ConfigSetting.Of(options.SecretsEnabled),

                // Send the persisted user preference (AutoScan), not the InternalAutoScan runtime
                // gate. The gate only delays the *first* scan until the IDE is ready (handled by the
                // scan-trigger logic in OnSnykConfiguration / OnHasAuthenticated) — it must not be the
                // value we tell the LS to persist, or a manual-mode choice gets overwritten by the
                // gate's post-first-scan `true` on the next config round-trip.
                [PflagKeys.ScanAutomatic] = ConfigSetting.Of(options.AutoScan),
                [PflagKeys.ScanNetNew] = ConfigSetting.Of(options.EnableDeltaFindings),

                [PflagKeys.SeverityFilterCritical] = ConfigSetting.Of(options.FilterCritical),
                [PflagKeys.SeverityFilterHigh] = ConfigSetting.Of(options.FilterHigh),
                [PflagKeys.SeverityFilterMedium] = ConfigSetting.Of(options.FilterMedium),
                [PflagKeys.SeverityFilterLow] = ConfigSetting.Of(options.FilterLow),

                [PflagKeys.IssueViewOpenIssues] = ConfigSetting.Of(options.OpenIssuesEnabled),
                [PflagKeys.IssueViewIgnoredIssues] = ConfigSetting.Of(options.IgnoredIssuesEnabled),

                [PflagKeys.ApiEndpoint] = ConfigSetting.Of(options.CustomEndpoint ?? string.Empty),
                [PflagKeys.Token] = ConfigSetting.Of(options.ApiToken?.ToString() ?? string.Empty),
                [PflagKeys.Organization] = ConfigSetting.Of(options.Organization ?? string.Empty),
                [PflagKeys.AuthenticationMethod] = ConfigSetting.Of(options.AuthenticationMethod.ToString().ToLowerInvariant()),
                [PflagKeys.ProxyInsecure] = ConfigSetting.Of(options.IgnoreUnknownCA),

                [PflagKeys.AutomaticDownload] = ConfigSetting.Of(options.BinariesAutoUpdate),
                [PflagKeys.CliPath] = ConfigSetting.Of(SnykCli.GetCliFilePath(options.CliCustomPath)),
                [PflagKeys.BinaryBaseUrl] = ConfigSetting.Of(options.CliBaseDownloadURL ?? string.Empty),
                [PflagKeys.CliReleaseChannel] = ConfigSetting.Of(options.CliReleaseChannel ?? string.Empty),

                [PflagKeys.TrustedFolders] = ConfigSetting.Of(options.TrustedFolders?.ToList() ?? new List<string>()),
                [PflagKeys.AdditionalEnvironment] = ConfigSetting.Of(options.AdditionalEnv ?? string.Empty),
                // LS applyCliConfig reads additional_parameters via settingStr (string type-assert),
                // so send as a space-joined string — same wire format LS uses on its outbound echo.
                [PflagKeys.AdditionalParameters] = ConfigSetting.Of(
                    string.Join(" ", options.AdditionalParameters ?? new List<string>())),

                [PflagKeys.DeviceId] = ConfigSetting.Of(options.DeviceId ?? string.Empty),
                [PflagKeys.ClientProtocolVersion] = ConfigSetting.Of("25"),
            };

            if (options.RiskScoreThreshold.HasValue)
                map[PflagKeys.RiskScoreThreshold] = ConfigSetting.Of(options.RiskScoreThreshold.Value);

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
