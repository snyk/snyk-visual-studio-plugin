// ABOUTME: JS-to-C# bridge for the settings panel's HTML page.
// ABOUTME: Invoked by WebView2MessageDispatcher after chrome.webview.postMessage routing.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Snyk.VisualStudio.Extension;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Extension;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// Handles configuration save / dirty-state / command messages posted from the
    /// LS-authored settings HTML. Method names retain their underscore-prefixed JS
    /// names because the dispatcher routes by exact string match.
    /// </summary>
    public class HtmlSettingsScriptingBridge
    {
        private static readonly ILogger Logger = LogManager.ForContext<HtmlSettingsScriptingBridge>();
        private readonly ISnykServiceProvider serviceProvider;
        private readonly Action onModified;
        private readonly Action onReset;
        private readonly Action<string> onAuthTokenChanged;
        private readonly Action<string, string> onCommandResult;
        private TaskCompletionSource<bool> saveCompletionTcs =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private ISnykOptions Options => serviceProvider.Options;
        private ISnykOptionsManager OptionsManager => serviceProvider.SnykOptionsManager;

        /// <summary>
        /// Completes when the most recent save attempt has finished (success or error).
        /// Caller must invoke <see cref="BeginSave"/> before triggering a new save so a
        /// fresh task is available to await.
        /// </summary>
        public Task<bool> SaveCompletion => saveCompletionTcs.Task;

        /// <summary>
        /// Resets <see cref="SaveCompletion"/> to a fresh incomplete task. Called by the
        /// settings window just before invoking the page's <c>getAndSaveIdeConfig()</c>.
        /// </summary>
        public void BeginSave()
        {
            saveCompletionTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public HtmlSettingsScriptingBridge(
            ISnykServiceProvider serviceProvider,
            Action onModified,
            Action onReset = null,
            Action<string> onAuthTokenChanged = null,
            Action<string, string> onCommandResult = null)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.onModified = onModified ?? throw new ArgumentNullException(nameof(onModified));
            this.onReset = onReset;
            this.onAuthTokenChanged = onAuthTokenChanged;
            this.onCommandResult = onCommandResult;
        }

        /// <summary>
        /// Routed from <c>window.__saveIdeConfig__(jsonString)</c>. The LS HTML
        /// handles all validation and data collection — we just persist the config. Always
        /// completes <see cref="SaveCompletion"/> (true on success, false on failure) so the
        /// caller can stop waiting and react to failure.
        /// <para>
        /// Failures are logged and signalled via <see cref="SaveCompletion"/> rather than
        /// re-thrown — the JS caller invoked us via <c>chrome.webview.postMessage</c>
        /// (fire-and-forget under WebView2) and can't observe an exception the way it could
        /// over the old IE COM bridge. <c>HtmlSettingsControl.SaveAsync</c> awaits
        /// <see cref="SaveCompletion"/> and surfaces failure to the user from there.
        /// </para>
        /// </summary>
        public void __saveIdeConfig__(string jsonString)
        {
            // Important: must NOT use a blocking JoinableTaskFactory.Run here. This bridge is
            // invoked from the WebView2 message dispatcher on the UI thread while OnApply's
            // own JoinableTaskFactory.Run is already blocking the UI thread waiting on
            // SaveCompletion. Nesting a second blocking Run on the same UI thread for work
            // that itself schedules UI-thread continuations (SettingsChanged → DidChangeConfigurationAsync,
            // LS round-trip via $/snyk.configuration in v25) freezes the dispatcher.
            // RunAsync + FireAndForget lets the bridge handler return immediately; the
            // outer OnApply unblocks when saveCompletionTcs is signalled below.
            // Snapshot the completion source: BeginSave() can swap saveCompletionTcs for a fresh
            // one (next save attempt) while this lambda is still in flight, and we must signal the
            // exact TCS that the matching SaveAsync is awaiting — not whatever is current later.
            var tcs = saveCompletionTcs;
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await ParseAndSaveConfigAsync(jsonString);

                    // Persist all settings to storage at the end.
                    // This triggers SettingsChanged event which notifies Language Server.
                    OptionsManager.Save(Options, triggerSettingsChangedEvent: true);

                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error saving configuration");
                    tcs.TrySetResult(false);
                }
            }).FireAndForget();
        }

        /// <summary>
        /// Called from LS HTML JavaScript: window.__onFormDirtyChange__(isDirty)
        /// Notifies the IDE when the form state changes.
        /// </summary>
        public void __onFormDirtyChange__(bool isDirty)
        {
            if (isDirty)
                onModified?.Invoke();
            else
                onReset?.Invoke();
        }

        /// <summary>
        /// Called from LS HTML JavaScript: window.__ideExecuteCommand__(command, argsJson, callbackId)
        /// Routes commands to the Language Server via workspace/executeCommand.
        /// If callbackId is non-empty, the command result is passed back to the JS callback.
        /// When command is "snyk.login" and args has 3+ elements, saves auth params to IDE storage
        /// immediately without triggering DidChangeConfigurationAsync.
        /// </summary>
        public void __ideExecuteCommand__(string command, string argsJson, string callbackId)
        {
            if (command == "snyk.login")
            {
                try
                {
                    var args = JsonConvert.DeserializeObject<object[]>(argsJson ?? "[]");
                    if (args != null && args.Length >= 3)
                    {
                        var authMethodStr = args[0]?.ToString() ?? string.Empty;
                        serviceProvider.Options.AuthenticationMethod = authMethodStr switch
                        {
                            "oauth" => AuthenticationType.OAuth,
                            "pat" => AuthenticationType.Pat,
                            "token" => AuthenticationType.Token,
                            _ => AuthenticationType.OAuth,
                        };

                        // Validate the endpoint coming from the (LS-served) page before persisting:
                        // only accept an absolute http/https URL, and allow empty to mean "reset to
                        // default". An empty string clears the override; anything else malformed is
                        // ignored so a page can't repoint the API host to a bogus value.
                        var endpoint = args[1]?.ToString() ?? string.Empty;
                        if (string.IsNullOrEmpty(endpoint))
                        {
                            serviceProvider.Options.CustomEndpoint = string.Empty;
                        }
                        else if (UriExtensions.IsValidWebUrl(endpoint))
                        {
                            serviceProvider.Options.CustomEndpoint = endpoint;
                        }
                        else
                        {
                            Logger.Warning("Ignoring snyk.login endpoint that is not an absolute http/https URL");
                        }

                        serviceProvider.Options.IgnoreUnknownCA = ParseJsBool(args[2]);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Failed to save login args from __ideExecuteCommand__");
                }
            }

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ExecuteCommandBridge.DispatchAsync(
                    serviceProvider.LanguageClientManager,
                    command,
                    argsJson,
                    callbackId,
                    onCommandResult,
                    SnykVSPackage.Instance.DisposalToken);
            }).FireAndForget();
        }

        // Deterministically coerce a JSON-deserialized arg to bool. JsonConvert yields bool, long,
        // or string for JSON primitives; Convert.ToBoolean throws on "yes"/null/numbers-as-string,
        // which previously surfaced as a silently-not-applied SSL toggle. Unknown shapes → false.
        // internal for testability (InternalsVisibleTo test project).
        internal static bool ParseJsBool(object value)
        {
            switch (value)
            {
                case null: return false;
                case bool b: return b;
                case long l: return l != 0;
                case int i: return i != 0;
                case string s:
                    return bool.TryParse(s, out var parsed) ? parsed : s == "1";
                default: return false;
            }
        }

        /// <summary>
        /// Called from LS HTML JavaScript: window.__ideSaveAttemptFinished__(status)
        /// Optional callback to track save attempt results.
        /// </summary>
        public void __ideSaveAttemptFinished__(string status)
        {
            // Snapshot the completion source up front, mirroring __saveIdeConfig__. It's a no-op while
            // this handler stays synchronous (the UI thread can't swap the field mid-method, and
            // BeginSave only ever runs on that thread), but keeps the pattern correct if a future edit
            // introduces an await before the signal below.
            var tcs = saveCompletionTcs;

            Logger.Information("Save attempt finished with status: {Status}", status);

            // Fast-fail the save when the page reports it did NOT persist (e.g. client-side
            // validation rejected the CLI version). In that case getAndSaveIdeConfig() returns
            // without calling __saveIdeConfig__, so SaveCompletion would otherwise sit unsignalled
            // until HtmlSettingsControl.SaveAsync's 5s timeout — leaving the Apply button hung and
            // then surfacing a misleading "check the log" error. A non-"success" status lets
            // OnApply report the failure immediately and keep the user on the page to fix it.
            // The success path is still driven by __saveIdeConfig__ → TrySetResult(true);
            // TrySetResult is first-wins, so a later "success" signal here is a harmless no-op.
            if (!string.IsNullOrEmpty(status) &&
                !string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            {
                tcs.TrySetResult(false);
            }
        }

        private async Task ParseAndSaveConfigAsync(string jsonString)
        {
            // LS HTML JavaScript handles all validation - we just parse and save.
            // Throw on a null result (malformed/empty JSON that maps to nothing) rather than
            // returning early: the caller would otherwise still run OptionsManager.Save and
            // signal success, so the user would see no error while every change is discarded.
            var config = JsonConvert.DeserializeObject<IdeConfigData>(jsonString);
            if (config == null)
            {
                // A null result means the JSON was empty/whitespace/"null" or otherwise mapped to
                // nothing, so it shouldn't carry a real token — but truncate defensively to keep the
                // log bounded and avoid dumping anything large/sensitive in full.
                var loggedPayload = jsonString ?? "(null)";
                if (loggedPayload.Length > 200)
                {
                    loggedPayload = loggedPayload.Substring(0, 200) + "…(truncated)";
                }

                Logger.Error(
                    "Settings payload from the settings page deserialized to null; no settings were saved. Payload: {Payload}",
                    loggedPayload);
                throw new InvalidOperationException(
                    "Could not parse settings payload from the settings page; no settings were saved.");
            }

            // Catch settings-HTML/plugin drift before applying: the form is synced from snyk-ls and
            // can add keys this build doesn't bind, which Newtonsoft would otherwise drop silently.
            // Warn (naming the keys) on a partial mismatch so the rest of the save still goes through,
            // and fail loudly when nothing is recognised (a wholesale rename that would no-op yet
            // report success).
            var contract = IdeConfigContract.Analyze(jsonString);
            if (contract.AllUnmapped)
            {
                throw new InvalidOperationException(
                    "None of the keys posted by the settings page are recognised by this plugin build (keys: " +
                    string.Join(", ", contract.UnmappedKeys) +
                    "). The settings HTML is likely newer than the plugin; no settings were saved.");
            }

            if (contract.HasUnmappedKeys)
            {
                Logger.Warning(
                    "Settings page posted key(s) this plugin build does not recognise and did not save " +
                    "(global: [{GlobalKeys}], per-folder: [{FolderKeys}]). " +
                    "The settings HTML (synced from snyk-ls) may be newer than this plugin.",
                    string.Join(", ", contract.UnmappedKeys),
                    string.Join(", ", contract.UnmappedFolderKeys));
            }

            var isCliOnly = config.IsFallbackForm ?? false;
            Logger.Information("Saving workspace configuration (CLI only: {IsCliOnly})", isCliOnly);

            // Apply directly to the live Options, but capture a rollback first. Apply* mutate Options
            // in place (folder-config entries included) and OptionsManager.Save only runs after this
            // method returns — so a mid-apply failure would otherwise leave Options half-applied in
            // memory, diverging from disk until restart. On failure we restore and rethrow.
            var rollback = SnapshotOptionsForRollback();
            try
            {
                // Always apply CLI settings and Insecure setting
                ApplyCliSettings(config);
                ApplyInsecureSetting(config);

                // Only apply full settings when not in CLI-only mode
                if (!isCliOnly)
                {
                    ApplyScanSettings(config);
                    ApplyIssueViewSettings(config);
                    var previousAuthMethod = Options.AuthenticationMethod;
                    ApplyAuthenticationSettings(config);
                    // Clear stored token when auth method changes: a token from one method is not valid for another.
                    if (config.AuthenticationMethod != null && Options.AuthenticationMethod != previousAuthMethod)
                    {
                        Options.ApiToken = new AuthenticationToken(Options.AuthenticationMethod, string.Empty);
                    }

                    ApplyConnectionSettings(config);
                    ApplyTrustedFolders(config);
                    ApplyFilterSettings(config);
                    ApplyMiscellaneousSettings(config);
                    await ApplyFolderConfigsAsync(config, jsonString);
                }
            }
            catch
            {
                rollback();
                throw;
            }
        }

        // Captures the current Options state and returns an action that restores it, so a partially-
        // applied save can be rolled back (see ParseAndSaveConfigAsync). Collections are deep-copied
        // because the apply mutates folder-config entries in place and replaces TrustedFolders.
        private Action SnapshotOptionsForRollback()
        {
            var o = Options;

            var deviceId = o.DeviceId;
            var autoScan = o.AutoScan;
            var openIssues = o.OpenIssuesEnabled;
            var ignoredIssues = o.IgnoredIssuesEnabled;
            var apiToken = o.ApiToken;
            var authMethod = o.AuthenticationMethod;
            var customEndpoint = o.CustomEndpoint;
            var organization = o.Organization;
            var ignoreUnknownCa = o.IgnoreUnknownCA;
            var ossEnabled = o.OssEnabled;
            var iacEnabled = o.IacEnabled;
            var codeEnabled = o.SnykCodeSecurityEnabled;
            var secretsEnabled = o.SecretsEnabled;
            var binariesAutoUpdate = o.BinariesAutoUpdate;
            var cliCustomPath = o.CliCustomPath;
            var cliReleaseChannel = o.CliReleaseChannel;
            var cliBaseDownloadUrl = o.CliBaseDownloadURL;
            var enableDelta = o.EnableDeltaFindings;
            var currentCliVersion = o.CurrentCliVersion;
            var analyticsSent = o.AnalyticsPluginInstalledSent;
            var filterCritical = o.FilterCritical;
            var filterHigh = o.FilterHigh;
            var filterMedium = o.FilterMedium;
            var filterLow = o.FilterLow;
            var additionalEnv = o.AdditionalEnv;
            var additionalParameters = o.AdditionalParameters != null ? new System.Collections.Generic.List<string>(o.AdditionalParameters) : null;
            var riskScoreThreshold = o.RiskScoreThreshold;
            var consistentIgnoresEnabled = o.ConsistentIgnoresEnabled;
            var folderConfigs = CloneFolderConfigs(o.FolderConfigs);
            var trustedFolders = o.TrustedFolders == null ? null : new HashSet<string>(o.TrustedFolders);

            return () =>
            {
                o.DeviceId = deviceId;
                o.AutoScan = autoScan;
                o.OpenIssuesEnabled = openIssues;
                o.IgnoredIssuesEnabled = ignoredIssues;
                o.ApiToken = apiToken;
                o.AuthenticationMethod = authMethod;
                o.CustomEndpoint = customEndpoint;
                o.Organization = organization;
                o.IgnoreUnknownCA = ignoreUnknownCa;
                o.OssEnabled = ossEnabled;
                o.IacEnabled = iacEnabled;
                o.SnykCodeSecurityEnabled = codeEnabled;
                o.SecretsEnabled = secretsEnabled;
                o.BinariesAutoUpdate = binariesAutoUpdate;
                o.CliCustomPath = cliCustomPath;
                o.CliReleaseChannel = cliReleaseChannel;
                o.CliBaseDownloadURL = cliBaseDownloadUrl;
                o.EnableDeltaFindings = enableDelta;
                o.CurrentCliVersion = currentCliVersion;
                o.AnalyticsPluginInstalledSent = analyticsSent;
                o.FilterCritical = filterCritical;
                o.FilterHigh = filterHigh;
                o.FilterMedium = filterMedium;
                o.FilterLow = filterLow;
                o.AdditionalEnv = additionalEnv;
                o.AdditionalParameters = additionalParameters;
                o.RiskScoreThreshold = riskScoreThreshold;
                o.ConsistentIgnoresEnabled = consistentIgnoresEnabled;
                o.FolderConfigs = folderConfigs;
                o.TrustedFolders = trustedFolders;
            };
        }

        private static List<FolderConfig> CloneFolderConfigs(List<FolderConfig> source)
        {
            if (source == null)
                return null;

            // Round-trip through JSON to deep-copy each entry, so in-place mutation during apply
            // can't corrupt the rollback snapshot.
            return JsonConvert.DeserializeObject<List<FolderConfig>>(JsonConvert.SerializeObject(source));
        }

        private void ApplyScanSettings(IdeConfigData config)
        {
            // Product enablement (snyk_oss_enabled, snyk_code_enabled, snyk_iac_enabled, snyk_secrets_enabled)
            if (config.SnykOssEnabled.HasValue)
            {
                Options.OssEnabled = config.SnykOssEnabled.Value;
            }

            if (config.SnykCodeEnabled.HasValue)
            {
                Options.SnykCodeSecurityEnabled = config.SnykCodeEnabled.Value;
            }

            if (config.SnykIacEnabled.HasValue)
            {
                Options.IacEnabled = config.SnykIacEnabled.Value;
            }

            if (config.SnykSecretsEnabled.HasValue)
            {
                Options.SecretsEnabled = config.SnykSecretsEnabled.Value;
            }

            // Apply automatic-scan toggle (scan_automatic)
            if (config.ScanAutomatic.HasValue)
            {
                Options.AutoScan = config.ScanAutomatic.Value;
            }
        }

        private void ApplyIssueViewSettings(IdeConfigData config)
        {
            // Apply issue view options (issue_view_open_issues, issue_view_ignored_issues)
            if (config.IssueViewOpenIssues.HasValue)
            {
                Options.OpenIssuesEnabled = config.IssueViewOpenIssues.Value;
            }

            if (config.IssueViewIgnoredIssues.HasValue)
            {
                Options.IgnoredIssuesEnabled = config.IssueViewIgnoredIssues.Value;
            }

            // Apply net-new / delta findings (scan_net_new)
            if (config.ScanNetNew.HasValue)
            {
                Options.EnableDeltaFindings = config.ScanNetNew.Value;
            }
        }

        private void ApplyAuthenticationSettings(IdeConfigData config)
        {
            // Apply authentication method (authenticationMethod: "oauth"/"token"/"pat")
            if (config.AuthenticationMethod != null)
            {
                var authMethodStr = config.AuthenticationMethod.ToLowerInvariant().Trim();
                switch (authMethodStr)
                {
                    case "oauth":
                        Options.AuthenticationMethod = AuthenticationType.OAuth;
                        break;
                    case "token":
                        Options.AuthenticationMethod = AuthenticationType.Token;
                        break;
                    case "pat":
                        Options.AuthenticationMethod = AuthenticationType.Pat;
                        break;
                    default:
                        // Default to OAuth if empty or unknown value
                        Options.AuthenticationMethod = AuthenticationType.OAuth;
                        break;
                }
            }
        }

        private void ApplyInsecureSetting(IdeConfigData config)
        {
            // Apply Insecure (SSL) setting - available in both CLI-only and full mode
            if (config.Insecure.HasValue)
            {
                Options.IgnoreUnknownCA = config.Insecure.Value;
            }
        }

        private void ApplyConnectionSettings(IdeConfigData config)
        {
            // Allow an empty value to reset to the default endpoint, but otherwise only accept an
            // absolute http/https URL — same guard as the snyk.login bridge path and the
            // OnHasAuthenticated LS callback — so a malformed or non-web URI (file:, javascript:, …)
            // can't be persisted as the API host.
            if (config.ApiEndpoint != null)
            {
                if (string.IsNullOrEmpty(config.ApiEndpoint) || UriExtensions.IsValidWebUrl(config.ApiEndpoint))
                {
                    Options.CustomEndpoint = config.ApiEndpoint;
                }
                else
                {
                    Logger.Warning("Ignoring settings endpoint that is not an absolute http/https URL");
                }
            }

            if (config.Token != null)
            {
                // Normalize empty/null/undefined to empty string for comparison
                var normalizedNewToken = config.Token?.Trim() ?? string.Empty;
                var normalizedExistingToken = Options.ApiToken?.ToString()?.Trim() ?? string.Empty;

                // Persist token only if it has changed
                if (normalizedNewToken != normalizedExistingToken)
                {
                    // Use the authenticationMethod from this request if provided, otherwise uses existing value
                    // Note: Requires caller to send authenticationMethod when updating token to ensure correct pairing.
                    // Store the trimmed value (what we compared) so stray form whitespace doesn't get
                    // baked into the token store and silently fail downstream IsValid() parsing.
                    Options.ApiToken = new AuthenticationToken(Options.AuthenticationMethod, normalizedNewToken);
                }
            }

            if (config.Organization != null)
            {
                Options.Organization = config.Organization;
            }
        }

        private void ApplyTrustedFolders(IdeConfigData config)
        {
            // Allow empty list to clear trusted folders
            if (config.TrustedFolders == null)
                return;

            var trustedFolders = new HashSet<string>();
            foreach (var folderPath in config.TrustedFolders)
            {
                if (!string.IsNullOrEmpty(folderPath))
                {
                    trustedFolders.Add(folderPath);
                }
            }

            // Set even if empty to allow clearing
            Options.TrustedFolders = trustedFolders;
        }

        private void ApplyCliSettings(IdeConfigData config)
        {
            // Allow empty values to reset settings
            if (config.CliPath != null)
            {
                Options.CliCustomPath = config.CliPath;
            }

            if (config.ManageBinariesAutomatically.HasValue)
            {
                Options.BinariesAutoUpdate = config.ManageBinariesAutomatically.Value;
            }

            if (config.CliBaseDownloadURL != null)
            {
                Options.CliBaseDownloadURL = config.CliBaseDownloadURL;
            }

            if (config.CliReleaseChannel != null)
            {
                Options.CliReleaseChannel = config.CliReleaseChannel;
            }
        }

        private void ApplyFilterSettings(IdeConfigData config)
        {
            // Severity filters arrive as individual flat keys (severity_filter_*). The form
            // only sends the ones that changed, so each is applied independently.
            if (config.SeverityFilterCritical.HasValue)
            {
                Options.FilterCritical = config.SeverityFilterCritical.Value;
            }

            if (config.SeverityFilterHigh.HasValue)
            {
                Options.FilterHigh = config.SeverityFilterHigh.Value;
            }

            if (config.SeverityFilterMedium.HasValue)
            {
                Options.FilterMedium = config.SeverityFilterMedium.Value;
            }

            if (config.SeverityFilterLow.HasValue)
            {
                Options.FilterLow = config.SeverityFilterLow.Value;
            }
        }

        private void ApplyMiscellaneousSettings(IdeConfigData config)
        {
            // Apply risk score threshold only when present — the form sends a changed-only
            // payload, so an absent value must not clobber the stored threshold with null.
            if (config.RiskScoreThreshold.HasValue)
            {
                Options.RiskScoreThreshold = config.RiskScoreThreshold;
            }

            // Global (Project Defaults) advanced settings — absent (null) means no change.
            if (config.AdditionalEnv != null)
            {
                Options.AdditionalEnv = config.AdditionalEnv;
            }
            if (config.AdditionalParameters != null)
            {
                Options.AdditionalParameters = config.AdditionalParameters
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }
        }

        private async Task ApplyFolderConfigsAsync(IdeConfigData config, string rawJson)
        {
            // Apply per-solution/folder settings (folderConfigs: [...])
            // Save to solution-specific storage AND update in-memory global FolderConfigs
            if (config.FolderConfigs != null && config.FolderConfigs.Count > 0)
            {
                await SaveFolderConfigsAsync(config.FolderConfigs);
            }
            // Detect folder fields sent as JSON null (the dialog's "Reset overrides" output). A
            // nullable field can't distinguish present-null from absent, so this re-reads the raw
            // JSON tree and flags the resets on the matching stored FolderConfig.
            ApplyFolderResetsFromRawJson(rawJson);
        }

        // Flags every folder field present in the raw JSON as an explicit null as a reset, then lets
        // snyk-ls decide which keys are actually resettable. snyk-ls is authoritative over folder-scoped
        // settings: it acts on the keys it recognizes and ignores nulls on the rest, so forwarding extra
        // present-nulls is a safe no-op and we don't maintain a duplicate whitelist here.
        private void ApplyFolderResetsFromRawJson(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
                return;

            JObject root;
            try
            {
                root = JObject.Parse(rawJson);
            }
            catch (JsonException ex)
            {
                Logger.Warning(ex, "Could not parse JSON for folder resets");
                return;
            }

            if (!(root["folderConfigs"] is JArray folderConfigsJson))
                return;

            var optionsConfigs = Options.FolderConfigs;
            if (optionsConfigs == null || optionsConfigs.Count == 0)
                return;

            foreach (var token in folderConfigsJson)
            {
                if (!(token is JObject folderObject))
                    continue;

                var folderPath = folderObject["folderPath"]?.Value<string>();

                var existingConfig = !string.IsNullOrEmpty(folderPath)
                    ? optionsConfigs.FirstOrDefault(fc => fc != null &&
                        string.Equals(fc.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase))
                    : (optionsConfigs.Count == 1 ? optionsConfigs[0] : null);
                if (existingConfig == null)
                    continue;

                foreach (var property in folderObject.Properties())
                {
                    if (property.Name == "folderPath")
                        continue;
                    if (property.Value.Type != JTokenType.Null)
                        continue;

                    // Flag the key so BuildFolderConfigs emits {value:null, changed:true} for it.
                    // snyk-ls is authoritative: it acts on the keys it recognizes as folder-scoped
                    // and ignores nulls on the rest, so forwarding every present-null is safe.
                    existingConfig.ResetKeys ??= new HashSet<string>();
                    existingConfig.ResetKeys.Add(property.Name);
                }
            }
        }

        private Task SaveFolderConfigsAsync(List<FolderConfigData> folderConfigs)
        {
            // The form posts a changed-only folder object (only the fields the user actually
            // touched, plus folderPath) per folder. The LS is the source of truth for folder
            // configs — it sends each workspace folder's config keyed by the path it registered,
            // which FolderConfigApplier stores in Options.FolderConfigs. We mirror the form's
            // changes into the matching stored entry by path.
            if (folderConfigs == null || folderConfigs.Count == 0)
                return Task.CompletedTask;

            try
            {
                var optionsConfigs = Options.FolderConfigs;
                if (optionsConfigs == null || optionsConfigs.Count == 0)
                {
                    Logger.Warning("Cannot save folder configs - no folder config available for the current workspace");
                    return Task.CompletedTask;
                }

                foreach (var folderConfig in folderConfigs)
                {
                    if (folderConfig == null) continue;

                    // Match each posted folder to its stored config BY PATH so multi-folder
                    // workspaces don't collapse every folder's edits onto a single entry. Both
                    // paths originate from the LS (the form is LS-rendered, the stored config from
                    // the LS config push), so exact case-insensitive equality is reliable. Fall
                    // back to the sole entry only when the payload omits the path (fallback form).
                    var existingConfig = !string.IsNullOrEmpty(folderConfig.FolderPath)
                        ? optionsConfigs.FirstOrDefault(fc => fc != null &&
                            string.Equals(fc.FolderPath, folderConfig.FolderPath, StringComparison.OrdinalIgnoreCase))
                        : (optionsConfigs.Count == 1 ? optionsConfigs[0] : null);
                    if (existingConfig == null) continue;

                    // Mirror the changed fields into the in-memory FolderConfig's opaque settings
                    // map so DidChangeConfiguration sends them to the LS (the LS is master for
                    // folder-config storage, incl. base branch which has no solution-storage slot of
                    // its own). Write each key only when the form posted it (changed-only PATCH) so a
                    // single-field edit doesn't blank out the siblings. Keyed by PflagKeys so it
                    // round-trips exactly as the LS expects. Present-null fields are handled
                    // separately by ApplyFolderResetsFromRawJson (they become ResetKeys, not values).
                    {
                        if (folderConfig.PreferredOrg != null)
                            existingConfig.Set(PflagKeys.PreferredOrg, folderConfig.PreferredOrg);
                        if (folderConfig.AutoDeterminedOrg != null)
                            existingConfig.Set(PflagKeys.AutoDeterminedOrg, folderConfig.AutoDeterminedOrg);
                        if (folderConfig.OrgSetByUser.HasValue)
                            existingConfig.Set(PflagKeys.OrgSetByUser, folderConfig.OrgSetByUser.Value);
                        if (folderConfig.AdditionalParameters != null)
                            existingConfig.Set(PflagKeys.AdditionalParameters, folderConfig.AdditionalParameters);
                        if (folderConfig.AdditionalEnv != null)
                            existingConfig.Set(PflagKeys.AdditionalEnvironment, folderConfig.AdditionalEnv);
                        if (folderConfig.BaseBranch != null)
                            existingConfig.Set(PflagKeys.BaseBranch, folderConfig.BaseBranch);
                        if (folderConfig.ScanCommandConfig != null)
                            existingConfig.Set(PflagKeys.ScanCommandConfig, folderConfig.ScanCommandConfig);

                        // Per-folder org-scope overrides (product enablement, severity, scan,
                        // issue view, risk score). Written so BuildFolderConfigs forwards them in
                        // the folder's settings map and the LS resolves folder-over-global.
                        if (folderConfig.SnykOssEnabled.HasValue)
                            existingConfig.Set(PflagKeys.SnykOssEnabled, folderConfig.SnykOssEnabled.Value);
                        if (folderConfig.SnykCodeEnabled.HasValue)
                            existingConfig.Set(PflagKeys.SnykCodeEnabled, folderConfig.SnykCodeEnabled.Value);
                        if (folderConfig.SnykIacEnabled.HasValue)
                            existingConfig.Set(PflagKeys.SnykIacEnabled, folderConfig.SnykIacEnabled.Value);
                        if (folderConfig.SnykSecretsEnabled.HasValue)
                            existingConfig.Set(PflagKeys.SnykSecretsEnabled, folderConfig.SnykSecretsEnabled.Value);
                        if (folderConfig.ScanAutomatic.HasValue)
                            existingConfig.Set(PflagKeys.ScanAutomatic, folderConfig.ScanAutomatic.Value);
                        if (folderConfig.ScanNetNew.HasValue)
                            existingConfig.Set(PflagKeys.ScanNetNew, folderConfig.ScanNetNew.Value);
                        if (folderConfig.SeverityFilterCritical.HasValue)
                            existingConfig.Set(PflagKeys.SeverityFilterCritical, folderConfig.SeverityFilterCritical.Value);
                        if (folderConfig.SeverityFilterHigh.HasValue)
                            existingConfig.Set(PflagKeys.SeverityFilterHigh, folderConfig.SeverityFilterHigh.Value);
                        if (folderConfig.SeverityFilterMedium.HasValue)
                            existingConfig.Set(PflagKeys.SeverityFilterMedium, folderConfig.SeverityFilterMedium.Value);
                        if (folderConfig.SeverityFilterLow.HasValue)
                            existingConfig.Set(PflagKeys.SeverityFilterLow, folderConfig.SeverityFilterLow.Value);
                        if (folderConfig.IssueViewOpenIssues.HasValue)
                            existingConfig.Set(PflagKeys.IssueViewOpenIssues, folderConfig.IssueViewOpenIssues.Value);
                        if (folderConfig.IssueViewIgnoredIssues.HasValue)
                            existingConfig.Set(PflagKeys.IssueViewIgnoredIssues, folderConfig.IssueViewIgnoredIssues.Value);
                        if (folderConfig.RiskScoreThreshold.HasValue)
                            existingConfig.Set(PflagKeys.RiskScoreThreshold, folderConfig.RiskScoreThreshold.Value);

                        Logger.Information("Mirrored folder config: {FolderPath}", existingConfig.FolderPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error saving folder configs");
                throw;
            }

            return Task.CompletedTask;
        }

    }
}
