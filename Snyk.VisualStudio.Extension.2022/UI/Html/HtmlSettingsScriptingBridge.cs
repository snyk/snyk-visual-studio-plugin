// ABOUTME: JS-to-C# bridge for the settings panel's HTML page.
// ABOUTME: Invoked by WebView2MessageDispatcher after chrome.webview.postMessage routing.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Serilog;
using Snyk.VisualStudio.Extension;
using Snyk.VisualStudio.Extension.Authentication;
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
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await ParseAndSaveConfigAsync(jsonString);

                    // Persist all settings to storage at the end.
                    // This triggers SettingsChanged event which notifies Language Server.
                    OptionsManager.Save(Options, triggerSettingsChangedEvent: true);

                    saveCompletionTcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error saving configuration");
                    saveCompletionTcs.TrySetResult(false);
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
                        serviceProvider.Options.CustomEndpoint = args[1]?.ToString() ?? string.Empty;
                        serviceProvider.Options.IgnoreUnknownCA = args[2] is bool b ? b : Convert.ToBoolean(args[2]);
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

        /// <summary>
        /// Called from LS HTML JavaScript: window.__ideSaveAttemptFinished__(status)
        /// Optional callback to track save attempt results.
        /// </summary>
        public void __ideSaveAttemptFinished__(string status)
        {
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
                saveCompletionTcs.TrySetResult(false);
            }
        }

        private async Task ParseAndSaveConfigAsync(string jsonString)
        {
            // LS HTML JavaScript handles all validation - we just parse and save
            var config = JsonConvert.DeserializeObject<IdeConfigData>(jsonString);
            if (config == null) return;

            var isCliOnly = config.IsFallbackForm ?? false;
            Logger.Information("Saving workspace configuration (CLI only: {IsCliOnly})", isCliOnly);

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
                await ApplyFolderConfigsAsync(config);
            }
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
            // Allow empty values to reset settings
            if (config.ApiEndpoint != null)
            {
                Options.CustomEndpoint = config.ApiEndpoint;
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
                    // Note: Requires caller to send authenticationMethod when updating token to ensure correct pairing
                    Options.ApiToken = new AuthenticationToken(Options.AuthenticationMethod, config.Token);
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
        }

        private async Task ApplyFolderConfigsAsync(IdeConfigData config)
        {
            // Apply per-solution/folder settings (folderConfigs: [...])
            // Save to solution-specific storage AND update in-memory global FolderConfigs
            if (config.FolderConfigs != null && config.FolderConfigs.Count > 0)
            {
                await SaveFolderConfigsAsync(config.FolderConfigs);
            }
        }

        private async Task SaveFolderConfigsAsync(List<FolderConfigData> folderConfigs)
        {
            // LS HTML sends folder configs for the current solution.
            // Pattern: save to solution-specific storage AND mirror the per-folder values into
            // the global Options.FolderConfigs slot.
            if (folderConfigs == null || folderConfigs.Count == 0)
                return;

            try
            {
                // Get current solution folder path
                var solutionPath = await serviceProvider.SolutionService.GetSolutionFolderAsync();
                if (string.IsNullOrEmpty(solutionPath))
                {
                    Logger.Warning("Cannot save folder configs - no solution loaded");
                    return;
                }

                // The form posts a changed-only folder object (only the fields the user
                // actually touched, plus folderPath). Apply each field only when it is present
                // so a single-field edit doesn't blank out the sibling folder settings.
                // Match via FolderConfigMatcher so this stays consistent with the LS->IDE path.
                var existingConfig = FolderConfigMatcher.FindFirstMatching(Options.FolderConfigs, solutionPath);

                foreach (var folderConfig in folderConfigs)
                {
                    if (folderConfig == null) continue;

                    // The VS integration is single-solution: our solution-scoped storage
                    // (SaveAdditionalEnvAsync etc.) and the mirror target below are both keyed to
                    // the current solution path. If the form ever posts entries for other folders,
                    // applying them here would clobber the current solution's settings with
                    // unrelated data, so skip any entry that doesn't match the current solution.
                    // Use the shared matcher (exact + normalisation-tolerant) so this agrees with
                    // the LS->IDE path. An absent folderPath keeps the legacy behaviour (the form
                    // omitted it; VS only has one folder anyway).
                    if (!string.IsNullOrEmpty(folderConfig.FolderPath) &&
                        !FolderConfigMatcher.Matches(folderConfig.FolderPath, solutionPath))
                    {
                        continue;
                    }

                    // 1. Persist the folder-scoped values that have solution-specific storage.
                    //    AdditionalParameters intentionally has no solution-storage slot — it flows
                    //    through FolderConfig.AdditionalParameters only (IDE-1714).
                    if (folderConfig.AdditionalEnv != null)
                    {
                        await OptionsManager.SaveAdditionalEnvAsync(folderConfig.AdditionalEnv);
                    }

                    if (folderConfig.PreferredOrg != null)
                    {
                        await OptionsManager.SavePreferredOrgAsync(folderConfig.PreferredOrg);
                    }

                    if (folderConfig.AutoDeterminedOrg != null)
                    {
                        await OptionsManager.SaveAutoDeterminedOrgAsync(folderConfig.AutoDeterminedOrg);
                    }

                    if (folderConfig.OrgSetByUser.HasValue)
                    {
                        await OptionsManager.SaveOrgSetByUserAsync(folderConfig.OrgSetByUser.Value);
                    }

                    // 2. Mirror the same changed fields into the in-memory global FolderConfigs
                    //    entry so DidChangeConfiguration sends the updated values to the LS (the LS
                    //    is master for folder-config storage, incl. base branch which has no
                    //    solution-storage slot of its own).
                    if (existingConfig != null)
                    {
                        if (folderConfig.PreferredOrg != null)
                            existingConfig.PreferredOrg = folderConfig.PreferredOrg;
                        if (folderConfig.AutoDeterminedOrg != null)
                            existingConfig.AutoDeterminedOrg = folderConfig.AutoDeterminedOrg;
                        if (folderConfig.OrgSetByUser.HasValue)
                            existingConfig.OrgSetByUser = folderConfig.OrgSetByUser.Value;
                        if (folderConfig.AdditionalParameters != null)
                            existingConfig.AdditionalParameters = folderConfig.AdditionalParameters;
                        if (folderConfig.AdditionalEnv != null)
                            existingConfig.AdditionalEnv = folderConfig.AdditionalEnv;
                        if (folderConfig.BaseBranch != null)
                            existingConfig.BaseBranch = folderConfig.BaseBranch;
                        if (folderConfig.ScanCommandConfig != null)
                            existingConfig.ScanCommandConfig = folderConfig.ScanCommandConfig;

                        // Per-folder org-scope overrides (product enablement, severity, scan,
                        // issue view, risk score). Mirrored so BuildFolderConfigs emits them in
                        // the folder's settings map and the LS resolves folder-over-global.
                        if (folderConfig.SnykOssEnabled.HasValue)
                            existingConfig.SnykOssEnabled = folderConfig.SnykOssEnabled;
                        if (folderConfig.SnykCodeEnabled.HasValue)
                            existingConfig.SnykCodeEnabled = folderConfig.SnykCodeEnabled;
                        if (folderConfig.SnykIacEnabled.HasValue)
                            existingConfig.SnykIacEnabled = folderConfig.SnykIacEnabled;
                        if (folderConfig.SnykSecretsEnabled.HasValue)
                            existingConfig.SnykSecretsEnabled = folderConfig.SnykSecretsEnabled;
                        if (folderConfig.ScanAutomatic.HasValue)
                            existingConfig.ScanAutomatic = folderConfig.ScanAutomatic;
                        if (folderConfig.ScanNetNew.HasValue)
                            existingConfig.ScanNetNew = folderConfig.ScanNetNew;
                        if (folderConfig.SeverityFilterCritical.HasValue)
                            existingConfig.SeverityFilterCritical = folderConfig.SeverityFilterCritical;
                        if (folderConfig.SeverityFilterHigh.HasValue)
                            existingConfig.SeverityFilterHigh = folderConfig.SeverityFilterHigh;
                        if (folderConfig.SeverityFilterMedium.HasValue)
                            existingConfig.SeverityFilterMedium = folderConfig.SeverityFilterMedium;
                        if (folderConfig.SeverityFilterLow.HasValue)
                            existingConfig.SeverityFilterLow = folderConfig.SeverityFilterLow;
                        if (folderConfig.IssueViewOpenIssues.HasValue)
                            existingConfig.IssueViewOpenIssues = folderConfig.IssueViewOpenIssues;
                        if (folderConfig.IssueViewIgnoredIssues.HasValue)
                            existingConfig.IssueViewIgnoredIssues = folderConfig.IssueViewIgnoredIssues;
                        if (folderConfig.RiskScoreThreshold.HasValue)
                            existingConfig.RiskScoreThreshold = folderConfig.RiskScoreThreshold;

                        Logger.Information("Mirrored folder config for solution: {SolutionPath}", solutionPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error saving folder configs");
                throw;
            }
        }

    }
}
