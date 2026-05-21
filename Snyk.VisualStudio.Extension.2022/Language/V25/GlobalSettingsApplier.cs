using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Serilog;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Language
{
    // Applies a pflag-keyed settings map (from $/snyk.configuration) to ISnykOptions.
    // PATCH semantics: absent or null-value entries are skipped (existing values preserved).
    internal static class GlobalSettingsApplier
    {
        private static readonly ILogger Logger = LogManager.ForContext(typeof(GlobalSettingsApplier));

        public static void Apply(Dictionary<string, ConfigSetting> settings, ISnykOptions options)
        {
            if (settings == null || settings.Count == 0) return;
            foreach (var kvp in settings)
                ApplyOne(kvp.Key, kvp.Value, options);
        }

        private static void ApplyOne(string key, ConfigSetting setting, ISnykOptions options)
        {
            if (setting?.Value == null) return;
            var val = setting.Value is JToken jt ? jt : JToken.FromObject(setting.Value);

            try
            {
                switch (key)
                {
                    case PflagKeys.SnykOssEnabled:       options.OssEnabled = val.Value<bool>(); break;
                    case PflagKeys.SnykCodeEnabled:      options.SnykCodeSecurityEnabled = val.Value<bool>(); break;
                    case PflagKeys.SnykIacEnabled:       options.IacEnabled = val.Value<bool>(); break;
                    case PflagKeys.SnykSecretsEnabled:   options.SecretsEnabled = val.Value<bool>(); break;

                    case PflagKeys.ScanAutomatic:        options.AutoScan = val.Value<bool>(); break;
                    case PflagKeys.ScanNetNew:           options.EnableDeltaFindings = val.Value<bool>(); break;

                    case PflagKeys.SeverityFilterCritical: options.FilterCritical = val.Value<bool>(); break;
                    case PflagKeys.SeverityFilterHigh:     options.FilterHigh     = val.Value<bool>(); break;
                    case PflagKeys.SeverityFilterMedium:   options.FilterMedium   = val.Value<bool>(); break;
                    case PflagKeys.SeverityFilterLow:      options.FilterLow      = val.Value<bool>(); break;

                    case PflagKeys.IssueViewOpenIssues:    options.OpenIssuesEnabled    = val.Value<bool>(); break;
                    case PflagKeys.IssueViewIgnoredIssues: options.IgnoredIssuesEnabled = val.Value<bool>(); break;

                    case PflagKeys.RiskScoreThreshold:   options.RiskScoreThreshold = val.Value<int?>(); break;

                    case PflagKeys.ApiEndpoint:          options.CustomEndpoint  = val.Value<string>(); break;
                    case PflagKeys.Organization:         options.Organization    = val.Value<string>(); break;
                    case PflagKeys.ProxyInsecure:        options.IgnoreUnknownCA = val.Value<bool>(); break;

                    case PflagKeys.AutomaticDownload:    options.BinariesAutoUpdate = val.Value<bool>(); break;
                    case PflagKeys.CliPath:              options.CliCustomPath      = val.Value<string>(); break;
                    case PflagKeys.BinaryBaseUrl:        options.CliBaseDownloadURL = val.Value<string>(); break;
                    case PflagKeys.CliReleaseChannel:    options.CliReleaseChannel  = val.Value<string>(); break;

                    case PflagKeys.AdditionalEnvironment: options.AdditionalEnv = val.Value<string>(); break;

                    case PflagKeys.TrustedFolders:
                        var list = val.ToObject<List<string>>();
                        options.TrustedFolders = new HashSet<string>(
                            list ?? Enumerable.Empty<string>());
                        break;

                    case PflagKeys.AuthenticationMethod:
                        options.AuthenticationMethod = ParseAuthMethod(val.Value<string>());
                        break;

                    // token is declared writeOnly:true in snyk-ls (ldx_sync_config.go) and is
                    // never included in $/snyk.configuration payloads sent to the IDE, so the
                    // ordering dependency on AuthenticationMethod is a non-issue in production.
                    // The case is kept for completeness if Apply is reused in other contexts.
                    case PflagKeys.Token:
                        var tokenStr = val.Value<string>();
                        if (tokenStr != null)
                            options.ApiToken = new AuthenticationToken(options.AuthenticationMethod, tokenStr);
                        break;

                    // Read-only / metadata keys sent by LS — safe to ignore on inbound.
                    case PflagKeys.ClientProtocolVersion:
                    case PflagKeys.DeviceId:
                    case PflagKeys.AutoDeterminedOrg:
                    case PflagKeys.OrgSetByUser:
                    case PflagKeys.BaseBranch:
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "GlobalSettingsApplier: failed to apply key '{Key}', skipping", key);
            }
        }

        private static AuthenticationType ParseAuthMethod(string method) =>
            (method?.ToLowerInvariant().Trim()) switch
            {
                "token" => AuthenticationType.Token,
                "pat"   => AuthenticationType.Pat,
                _       => AuthenticationType.OAuth,
            };
    }
}
