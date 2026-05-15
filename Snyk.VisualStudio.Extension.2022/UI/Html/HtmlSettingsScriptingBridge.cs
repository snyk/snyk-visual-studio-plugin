// ABOUTME: JavaScript-to-C# bridge for WebBrowser control using ObjectForScripting
// ABOUTME: Handles configuration saves and state changes from HTML form

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
    /// COM-visible bridge object for JavaScript interaction.
    /// Must be ComVisible for IE11 WebBrowser control's ObjectForScripting.
    /// </summary>
    [ComVisible(true)]
    public class HtmlSettingsScriptingBridge
    {
        private static readonly ILogger Logger = LogManager.ForContext<HtmlSettingsScriptingBridge>();
        private readonly ISnykServiceProvider serviceProvider;
        private readonly Action onModified;
        private readonly Action onReset;
        private readonly Action<string> onAuthTokenChanged;
        private readonly Action<string, string> onCommandResult;
        private volatile bool isSaveComplete;

        private ISnykOptions Options => serviceProvider.Options;
        private ISnykOptionsManager OptionsManager => serviceProvider.SnykOptionsManager;

        /// <summary>
        /// Indicates whether the most recent save operation has completed.
        /// Used by HtmlSettingsWindow to wait for save completion.
        /// Volatile to ensure thread-safe reads from UI thread while writes happen on background threads.
        /// </summary>
        public bool IsSaveComplete
        {
            get => isSaveComplete;
            private set => isSaveComplete = value;
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
        /// Called from LS HTML JavaScript: window.__saveIdeConfig__(jsonString)
        /// The LS HTML handles all validation and data collection - we just save the config.
        /// </summary>
        public void __saveIdeConfig__(string jsonString)
        {
            try
            {
                IsSaveComplete = false;

                // Parse and apply all configuration changes
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ParseAndSaveConfigAsync(jsonString);

                    // Persist all settings to storage at the end
                    // This triggers SettingsChanged event which notifies Language Server
                    OptionsManager.Save(Options, triggerSettingsChangedEvent: true);

                    IsSaveComplete = true;
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error saving configuration");
                IsSaveComplete = true; // Unblock even on error
                throw; // Re-throw so JavaScript can handle
            }
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
            // Apply scan settings (activateSnykOpenSource, activateSnykCode, activateSnykIac)
            if (config.ActivateSnykOpenSource.HasValue)
            {
                Options.OssEnabled = config.ActivateSnykOpenSource.Value;
            }

            if (config.ActivateSnykCode.HasValue)
            {
                Options.SnykCodeSecurityEnabled = config.ActivateSnykCode.Value;
            }

            if (config.ActivateSnykIac.HasValue)
            {
                Options.IacEnabled = config.ActivateSnykIac.Value;
            }

            // Accept secrets flag as camelCase (activateSnykSecrets) or snake_case (snyk_secrets_enabled);
            // camelCase takes precedence.
            var secretsValue = config.ActivateSnykSecrets ?? config.SnykSecretsEnabledFallback;
            if (secretsValue.HasValue)
            {
                Options.SecretsEnabled = secretsValue.Value;
            }

            // Apply scanning mode (auto/manual)
            if (config.ScanningMode != null)
            {
                Options.AutoScan = config.ScanningMode == "auto";
            }
        }

        private void ApplyIssueViewSettings(IdeConfigData config)
        {
            // Apply issue view options (issueViewOptions: {openIssues, ignoredIssues})
            if (config.IssueViewOptions != null)
            {
                Options.OpenIssuesEnabled = config.IssueViewOptions.OpenIssues;
                Options.IgnoredIssuesEnabled = config.IssueViewOptions.IgnoredIssues;
            }

            // Apply delta findings (enableDeltaFindings: "true"/"false" or boolean)
            if (config.EnableDeltaFindings.HasValue)
            {
                Options.EnableDeltaFindings = config.EnableDeltaFindings.Value;
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
            if (config.Endpoint != null)
            {
                Options.CustomEndpoint = config.Endpoint;
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
            if (config.FilterSeverity == null)
                return;

            Options.FilterCritical = config.FilterSeverity.Critical;
            Options.FilterHigh = config.FilterSeverity.High;
            Options.FilterMedium = config.FilterSeverity.Medium;
            Options.FilterLow = config.FilterSeverity.Low;
        }

        private void ApplyMiscellaneousSettings(IdeConfigData config)
        {
            // Apply risk score threshold
            Options.RiskScoreThreshold = config.RiskScoreThreshold;
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
            // LS HTML sends folder configs for the current solution
            // Pattern: Save to solution-specific storage AND update in-memory global FolderConfigs
            // (matches SnykGeneralOptionsDialogPage.UpdateFolderConfigForCurrentSolutionAsync)
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

                foreach (var folderConfig in folderConfigs)
                {
                    if (folderConfig == null) continue;

                    // Extract folder settings (matching LS HTML JSON keys)
                    var additionalOptions = string.Join(" ", folderConfig.AdditionalParameters);
                    var additionalEnv = folderConfig.AdditionalEnv ?? string.Empty;
                    var preferredOrg = folderConfig.PreferredOrg ?? string.Empty;
                    var autoOrg = folderConfig.AutoDeterminedOrg ?? string.Empty;
                    var orgSetByUser = folderConfig.OrgSetByUser;

                    // 1. Save to solution-specific storage
                    // Note: AdditionalOptions not saved here - flows through FolderConfig.AdditionalParameters only (IDE-1714)
                    await OptionsManager.SaveAdditionalEnvAsync(additionalEnv);
                    await OptionsManager.SavePreferredOrgAsync(preferredOrg);
                    await OptionsManager.SaveAutoDeterminedOrgAsync(autoOrg);
                    await OptionsManager.SaveOrgSetByUserAsync(orgSetByUser);

                    // 2. Update in-memory global FolderConfigs to mirror solution settings (only if already exists)
                    if (Options.FolderConfigs != null)
                    {
                        var existingConfig = Options.FolderConfigs.FirstOrDefault(fc =>
                            string.Equals(fc.FolderPath, solutionPath, StringComparison.OrdinalIgnoreCase));

                        if (existingConfig != null)
                        {
                            // Mirror all properties from JSON to existing config
                            existingConfig.PreferredOrg = preferredOrg;
                            existingConfig.AutoDeterminedOrg = autoOrg;
                            existingConfig.OrgSetByUser = orgSetByUser;
                            existingConfig.AdditionalParameters = folderConfig.AdditionalParameters ?? new List<string>();
                            existingConfig.AdditionalEnv = additionalEnv;
                            if (folderConfig.ScanCommandConfig != null)
                            {
                                existingConfig.ScanCommandConfig = folderConfig.ScanCommandConfig;
                            }

                            Logger.Information("Mirrored folder config for solution: {SolutionPath}", solutionPath);
                        }
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
