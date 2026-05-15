using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Language
{
    // Converts ISnykOptions to the v25 pflag-keyed wire shape.
    // Not wired into SnykLanguageClient yet — that happens behind V25Feature.Enabled in PR-3.
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
                RequiredProtocolVersion = LsConstants.ProtocolVersion,
                DeviceId = options.DeviceId,
                IntegrationName = GetIntegrationName(options),
                IntegrationVersion = GetIntegrationVersion(options),
                OsPlatform = Environment.OSVersion.Platform.ToString(),
                OsArch = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(),
                RuntimeName = ".NET Framework",
                RuntimeVersion = Environment.Version.ToString(),
                HoverVerbosity = 1,
                OutputFormat = "plain",
            };
        }

        public Dictionary<string, ConfigSetting> BuildSettingsMap(ISnykOptions options)
        {
#pragma warning disable VSTHRD104
            var additionalParams = ThreadHelper.JoinableTaskFactory.Run(
                () => serviceProvider.SnykOptionsManager.GetAdditionalOptionsAsync());
#pragma warning restore VSTHRD104

            var map = new Dictionary<string, ConfigSetting>
            {
                [PflagKeys.SnykOssEnabled] = ConfigSetting.Of(options.OssEnabled),
                [PflagKeys.SnykCodeEnabled] = ConfigSetting.Of(options.SnykCodeSecurityEnabled),
                [PflagKeys.SnykIacEnabled] = ConfigSetting.Of(options.IacEnabled),
                [PflagKeys.SnykSecretsEnabled] = ConfigSetting.Of(options.SecretsEnabled),

                [PflagKeys.ScanAutomatic] = ConfigSetting.Of(options.InternalAutoScan),
                [PflagKeys.ScanNetNew] = ConfigSetting.Of(options.EnableDeltaFindings),

                [PflagKeys.SeverityFilterCritical] = ConfigSetting.Of(options.FilterCritical),
                [PflagKeys.SeverityFilterHigh] = ConfigSetting.Of(options.FilterHigh),
                [PflagKeys.SeverityFilterMedium] = ConfigSetting.Of(options.FilterMedium),
                [PflagKeys.SeverityFilterLow] = ConfigSetting.Of(options.FilterLow),

                [PflagKeys.IssueViewOpenIssues] = ConfigSetting.Of(options.OpenIssuesEnabled),
                [PflagKeys.IssueViewIgnoredIssues] = ConfigSetting.Of(options.IgnoredIssuesEnabled),

                [PflagKeys.ApiEndpoint] = ConfigSetting.Of(options.CustomEndpoint ?? string.Empty),
                [PflagKeys.Token] = ConfigSetting.Of(options.ApiToken.ToString()),
                [PflagKeys.Organization] = ConfigSetting.Of(options.Organization ?? string.Empty),
                [PflagKeys.AuthenticationMethod] = ConfigSetting.Of(options.AuthenticationMethod.ToString().ToLowerInvariant()),
                [PflagKeys.ProxyInsecure] = ConfigSetting.Of(options.IgnoreUnknownCA),

                [PflagKeys.AutomaticDownload] = ConfigSetting.Of(options.BinariesAutoUpdate),
                [PflagKeys.CliPath] = ConfigSetting.Of(SnykCli.GetCliFilePath(options.CliCustomPath)),
                [PflagKeys.BinaryBaseUrl] = ConfigSetting.Of(options.CliBaseDownloadURL ?? string.Empty),
                [PflagKeys.CliReleaseChannel] = ConfigSetting.Of(options.CliReleaseChannel ?? string.Empty),

                [PflagKeys.TrustedFolders] = ConfigSetting.Of(options.TrustedFolders?.ToList() ?? new List<string>()),
                [PflagKeys.AdditionalParameters] = ConfigSetting.Of(additionalParams ?? string.Empty),
                [PflagKeys.AdditionalEnvironment] = ConfigSetting.Of(options.AdditionalEnv ?? string.Empty),

                [PflagKeys.DeviceId] = ConfigSetting.Of(options.DeviceId ?? string.Empty),
                [PflagKeys.ClientProtocolVersion] = ConfigSetting.Of(LsConstants.ProtocolVersion),
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
                var settings = new Dictionary<string, ConfigSetting>();

                if (fc.AdditionalParameters != null)
                    settings[PflagKeys.AdditionalParameters] = ConfigSetting.Of(fc.AdditionalParameters);
                if (fc.AdditionalEnv != null)
                    settings[PflagKeys.AdditionalEnvironment] = ConfigSetting.Of(fc.AdditionalEnv);
                if (fc.PreferredOrg != null)
                    settings[PflagKeys.PreferredOrg] = ConfigSetting.Of(fc.PreferredOrg);
                settings[PflagKeys.OrgSetByUser] = ConfigSetting.Of(fc.OrgSetByUser);
                if (fc.AutoDeterminedOrg != null)
                    settings[PflagKeys.AutoDeterminedOrg] = ConfigSetting.Of(fc.AutoDeterminedOrg);
                if (fc.BaseBranch != null)
                    settings[PflagKeys.BaseBranch] = ConfigSetting.Of(fc.BaseBranch);
                if (fc.ScanCommandConfig != null)
                    settings[PflagKeys.ScanCommandConfig] = ConfigSetting.Of(fc.ScanCommandConfig);

                result.Add(new LspFolderConfig
                {
                    FolderPath = fc.FolderPath,
                    Settings = settings.Count > 0 ? settings : null,
                });
            }
            return result;
        }

        private string GetIntegrationName(ISnykOptions options) =>
            $"{options.IntegrationEnvironment}@@{options.IntegrationName}";

        private string GetIntegrationVersion(ISnykOptions options) =>
            $"{options.IntegrationEnvironmentVersion}@@{options.IntegrationVersion}";
    }
}
