using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Serilog;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Language
{
    // Applies a pflag-keyed settings map (from $/snyk.configuration) to ISnykOptions.
    // PATCH semantics: absent, null, and (for the two keys noted below) empty entries are skipped.
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
                    // Not empty-guarded: an empty path means "use the default CLI location".
                    case PflagKeys.CliPath:              options.CliCustomPath      = NormaliseCliPath(val.Value<string>()); break;

                    // The language server registers these two with empty defaults and echoes every
                    // machine-scope setting, so an empty value means "no opinion", not "cleared".
                    case PflagKeys.BinaryBaseUrl:
                        var baseUrl = val.Value<string>();
                        if (!string.IsNullOrWhiteSpace(baseUrl)) options.CliBaseDownloadURL = baseUrl;
                        break;
                    case PflagKeys.CliReleaseChannel:
                        var releaseChannel = val.Value<string>();
                        if (!string.IsNullOrWhiteSpace(releaseChannel)) options.CliReleaseChannel = releaseChannel;
                        break;

                    case PflagKeys.AdditionalEnvironment:  options.AdditionalEnv        = val.Value<string>(); break;
                    case PflagKeys.AdditionalParameters:
                        // LS sends additional_parameters as a space-joined string (strings.Join(..., " ")),
                        // not a JSON array. Split on spaces; fall back to array deserialization for future-proofing.
                        options.AdditionalParameters = val.Type == JTokenType.String
                            ? (val.Value<string>() ?? string.Empty)
                                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                .ToList()
                            : val.ToObject<List<string>>();
                        break;

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

                    // Any other inbound key (e.g. sendErrorReports, enableTelemetry, snykCodeApi,
                    // path, automaticAuthentication) is intentionally not applied to IDE Options:
                    // none is surfaced in the VS settings UI or owned by the IDE, so the LS echo is
                    // ignored. Product enablement is handled above via the snyk_*_enabled pflags;
                    // the legacy camelCase "activateSnyk*" keys are not part of the v25 contract.
                    // The default arm is a deliberate no-op (the catch below still guards parse errors).
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "GlobalSettingsApplier: failed to apply key '{Key}', skipping", key);
            }
        }

        /// <summary>
        /// Collapses an inbound path that is already the IDE's own default CLI location back to
        /// empty. We send the resolved path outbound, so the LS echoes it straight back; storing it
        /// verbatim would turn "no custom path" into an explicit one in settings.json and in the
        /// settings UI, and pin the location even if the app-data directory later moves.
        /// </summary>
        private static string NormaliseCliPath(string cliPath)
        {
            if (string.IsNullOrWhiteSpace(cliPath))
            {
                return cliPath;
            }

            try
            {
                var defaultPath = SnykCli.GetSnykCliDefaultPath();

                return string.Equals(Path.GetFullPath(cliPath.Trim()), Path.GetFullPath(defaultPath), StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : cliPath;
            }
            catch (Exception ex)
            {
                // An unusable path (invalid characters, too long) cannot be the default, so keep it
                // as sent and let the CLI-not-found handling surface it.
                Logger.Warning(ex, "GlobalSettingsApplier: could not compare inbound cli_path against the default location");
                return cliPath;
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
