// ABOUTME: JavaScript-to-C# bridge for WebBrowser control using ObjectForScripting
// ABOUTME: Handles configuration saves and state changes from HTML form

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public class ConfigScriptingBridge
    {
        private static readonly ILogger Logger = LogManager.ForContext<ConfigScriptingBridge>();
        private readonly ISnykOptions options;
        private readonly ISnykOptionsManager optionsManager;
        private readonly ISnykServiceProvider serviceProvider;
        private readonly Action onModified;
        private readonly Action onReset;
        private volatile bool isSaveComplete;

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

        public ConfigScriptingBridge(
            ISnykOptions options,
            Action onModified,
            Action onReset = null,
            ISnykOptionsManager optionsManager = null,
            ISnykServiceProvider serviceProvider = null)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.onModified = onModified ?? throw new ArgumentNullException(nameof(onModified));
            this.onReset = onReset;
            this.optionsManager = optionsManager ?? throw new ArgumentNullException(nameof(optionsManager));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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
                    optionsManager.Save(options, triggerSettingsChangedEvent: true);

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
        /// Called from LS HTML JavaScript: window.__ideLogin__()
        /// Triggers the IDE's authentication flow via Language Server.
        /// </summary>
        public void __ideLogin__()
        {
            try
            {
                // Trigger authentication through the GeneralOptionsDialogPage
                // This matches the pattern from SnykGeneralSettingsUserControl
                if (serviceProvider?.GeneralOptionsDialogPage != null)
                {
                    serviceProvider.GeneralOptionsDialogPage.Authenticate();
                    Logger.Information("Authentication initiated from HTML settings");
                }
                else
                {
                    Logger.Warning("Cannot authenticate: GeneralOptionsDialogPage not available");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during authentication from HTML settings");
            }
        }

        /// <summary>
        /// Called from LS HTML JavaScript: window.__ideLogout__()
        /// Clears authentication token and notifies Language Server.
        /// </summary>
        public void __ideLogout__()
        {
            try
            {
                // Clear the API token
                options.ApiToken = AuthenticationToken.EmptyToken;

                // Notify Language Server of logout
                if (serviceProvider?.LanguageClientManager != null)
                {
                    ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await serviceProvider.LanguageClientManager.InvokeLogout(
                            SnykVSPackage.Instance.DisposalToken);
                        Logger.Information("Logout completed - Language Server notified");
                    }).FireAndForget();
                }
                else
                {
                    Logger.Information("Logout completed - Language Server not available");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during logout from HTML settings");
            }
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
            var config = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (config == null) return;

            ApplyScanSettings(config);
            ApplyIssueViewSettings(config);
            ApplyAuthenticationSettings(config);
            ApplyConnectionSettings(config);
            ApplyTrustedFolders(config);
            ApplyCliSettings(config);
            ApplyFilterSettings(config);
            ApplyMiscellaneousSettings(config);
            await ApplyFolderConfigsAsync(config);

            // Note: The following settings are LS-only and not persisted in IDE options:
            // - enableTrustedFoldersFeature: "false"
        }

        private void ApplyScanSettings(Dictionary<string, object> config)
        {
            // Apply scan settings (activateSnykOpenSource, activateSnykCode, activateSnykIac)
            if (HasScanTypeKeys(config))
            {
                options.OssEnabled = GetBoolean(config, "activateSnykOpenSource", false);
                options.SnykCodeSecurityEnabled = GetBoolean(config, "activateSnykCode", false);
                options.IacEnabled = GetBoolean(config, "activateSnykIac", false);
            }

            // Apply scanning mode (auto/manual)
            if (config.TryGetValue("scanningMode", out var mode))
            {
                options.AutoScan = mode?.ToString() == "auto";
            }
        }

        private void ApplyIssueViewSettings(Dictionary<string, object> config)
        {
            // Apply issue view options (issueViewOptions: {openIssues, ignoredIssues})
            if (config.TryGetValue("issueViewOptions", out var issueViewOptionsObj) && issueViewOptionsObj is JObject issueViewOptions)
            {
                if (issueViewOptions.TryGetValue("openIssues", out var openIssuesToken))
                {
                    options.OpenIssuesEnabled = openIssuesToken.Value<bool>();
                }
                if (issueViewOptions.TryGetValue("ignoredIssues", out var ignoredIssuesToken))
                {
                    options.IgnoredIssuesEnabled = ignoredIssuesToken.Value<bool>();
                }
            }

            // Apply delta findings (enableDeltaFindings: "true"/"false" or boolean)
            if (config.ContainsKey("enableDeltaFindings"))
            {
                options.EnableDeltaFindings = GetBoolean(config, "enableDeltaFindings", false);
            }
        }

        private void ApplyAuthenticationSettings(Dictionary<string, object> config)
        {
            // Apply authentication method (authenticationMethod: "oauth"/"token"/"pat")
            if (config.TryGetValue("authenticationMethod", out var authMethod))
            {
                var authMethodStr = authMethod?.ToString()?.ToLowerInvariant();
                switch (authMethodStr)
                {
                    case "oauth":
                        options.AuthenticationMethod = AuthenticationType.OAuth;
                        break;
                    case "token":
                        options.AuthenticationMethod = AuthenticationType.Token;
                        break;
                    case "pat":
                        options.AuthenticationMethod = AuthenticationType.Pat;
                        break;
                }
            }
        }

        private void ApplyConnectionSettings(Dictionary<string, object> config)
        {
            if (config.TryGetValue("endpoint", out var endpoint))
            {
                options.CustomEndpoint = endpoint?.ToString() ?? string.Empty;
            }

            if (config.TryGetValue("token", out var token))
            {
                var tokenString = token?.ToString();
                if (!string.IsNullOrEmpty(tokenString))
                {
                    // Use the authenticationMethod from this request if provided, otherwise uses existing value
                    // Note: Requires caller to send authenticationMethod when updating token to ensure correct pairing
                    options.ApiToken = new AuthenticationToken(options.AuthenticationMethod, tokenString);
                }
            }

            if (config.TryGetValue("organization", out var org))
            {
                options.Organization = org?.ToString() ?? string.Empty;
            }

            if (config.ContainsKey("insecure"))
            {
                options.IgnoreUnknownCA = GetBoolean(config, "insecure", false);
            }
        }

        private void ApplyTrustedFolders(Dictionary<string, object> config)
        {
            if (!config.TryGetValue("trustedFolders", out var trustedFoldersObj) || !(trustedFoldersObj is JArray trustedFoldersArray))
                return;

            var trustedFolders = new HashSet<string>();
            foreach (var folderToken in trustedFoldersArray)
            {
                var folderPath = folderToken?.ToString();
                if (!string.IsNullOrEmpty(folderPath))
                {
                    trustedFolders.Add(folderPath);
                }
            }

            if (trustedFolders.Count > 0)
            {
                options.TrustedFolders = trustedFolders;
            }
        }

        private void ApplyCliSettings(Dictionary<string, object> config)
        {
            if (config.TryGetValue("cliPath", out var cliPath))
            {
                options.CliCustomPath = cliPath?.ToString() ?? string.Empty;
            }

            if (config.ContainsKey("manageBinariesAutomatically"))
            {
                options.BinariesAutoUpdate = GetBoolean(config, "manageBinariesAutomatically", true);
            }

            if (config.TryGetValue("baseUrl", out var baseUrl) && !string.IsNullOrEmpty(baseUrl?.ToString()))
            {
                options.CliDownloadUrl = baseUrl.ToString();
            }

            if (config.TryGetValue("cliReleaseChannel", out var channel) && !string.IsNullOrEmpty(channel?.ToString()))
            {
                options.CliReleaseChannel = channel.ToString();
            }
        }

        private void ApplyFilterSettings(Dictionary<string, object> config)
        {
            if (!config.TryGetValue("filterSeverity", out var filterSeverityObj) || !(filterSeverityObj is JObject filterSeverity))
                return;

            if (filterSeverity.TryGetValue("critical", out var criticalToken))
            {
                options.FilterCritical = criticalToken.Value<bool>();
            }
            if (filterSeverity.TryGetValue("high", out var highToken))
            {
                options.FilterHigh = highToken.Value<bool>();
            }
            if (filterSeverity.TryGetValue("medium", out var mediumToken))
            {
                options.FilterMedium = mediumToken.Value<bool>();
            }
            if (filterSeverity.TryGetValue("low", out var lowToken))
            {
                options.FilterLow = lowToken.Value<bool>();
            }
        }

        private void ApplyMiscellaneousSettings(Dictionary<string, object> config)
        {
            // Apply additional environment variables
            if (config.TryGetValue("additionalEnv", out var additionalEnv))
            {
                options.AdditionalEnv = additionalEnv?.ToString() ?? string.Empty;
            }

            // Apply risk score threshold
            if (config.TryGetValue("riskScoreThreshold", out var riskScoreThreshold))
            {
                if (riskScoreThreshold != null && int.TryParse(riskScoreThreshold.ToString(), out var threshold))
                {
                    options.RiskScoreThreshold = threshold;
                }
                else
                {
                    options.RiskScoreThreshold = null;
                }
            }
        }

        private async Task ApplyFolderConfigsAsync(Dictionary<string, object> config)
        {
            // Apply per-solution/folder settings (folderConfigs: [...])
            // Save to solution-specific storage AND update in-memory global FolderConfigs
            if (config.TryGetValue("folderConfigs", out var folderConfigsObj) && folderConfigsObj is JArray folderConfigs)
            {
                await SaveFolderConfigsAsync(folderConfigs);
            }
        }

        private async Task SaveFolderConfigsAsync(JArray folderConfigs)
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

                foreach (var folderConfigToken in folderConfigs)
                {
                    var folderConfig = folderConfigToken.ToObject<Dictionary<string, object>>();
                    if (folderConfig == null) continue;

                    // Extract folder settings (matching LS HTML JSON keys)
                    var additionalOptions = folderConfig.TryGetValue("additionalParameters", out var addOpts)
                        ? addOpts?.ToString() ?? string.Empty
                        : string.Empty;

                    var additionalEnv = folderConfig.TryGetValue("additionalEnv", out var addEnv)
                        ? addEnv?.ToString() ?? string.Empty
                        : string.Empty;

                    var preferredOrg = folderConfig.TryGetValue("preferredOrg", out var prefOrg)
                        ? prefOrg?.ToString() ?? string.Empty
                        : string.Empty;

                    var autoOrg = folderConfig.TryGetValue("autoDeterminedOrg", out var autOrg)
                        ? autOrg?.ToString() ?? string.Empty
                        : string.Empty;

                    var orgSetByUser = folderConfig.TryGetValue("orgSetByUser", out var orgSetByUserObj)
                        ? GetBooleanFromObject(orgSetByUserObj)
                        : false;

                    // 1. Save to solution-specific storage
                    await optionsManager.SaveAdditionalOptionsAsync(additionalOptions);
                    await optionsManager.SaveAdditionalEnvAsync(additionalEnv);
                    await optionsManager.SavePreferredOrgAsync(preferredOrg);
                    await optionsManager.SaveAutoDeterminedOrgAsync(autoOrg);
                    await optionsManager.SaveOrgSetByUserAsync(orgSetByUser);

                    // 2. Update in-memory global FolderConfigs to mirror solution settings (only if already exists)
                    if (options.FolderConfigs != null)
                    {
                        var existingConfig = options.FolderConfigs.FirstOrDefault(fc =>
                            string.Equals(fc.FolderPath, solutionPath, StringComparison.OrdinalIgnoreCase));

                        if (existingConfig != null)
                        {
                            // Mirror all properties from JSON to existing config
                            existingConfig.PreferredOrg = preferredOrg;
                            existingConfig.AutoDeterminedOrg = autoOrg;
                            existingConfig.OrgSetByUser = orgSetByUser;

                            // Mirror AdditionalParameters (convert from string to List<string>)
                            if (!string.IsNullOrEmpty(additionalOptions))
                            {
                                // Split by space, but this is a simplified approach
                                // The LS HTML sends it as a string, we store it as List<string>
                                existingConfig.AdditionalParameters = additionalOptions.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                            }
                            else
                            {
                                existingConfig.AdditionalParameters = new List<string>();
                            }

                            // Mirror AdditionalEnv
                            existingConfig.AdditionalEnv = additionalEnv;

                            // Mirror ScanCommandConfig if present in JSON
                            if (folderConfig.TryGetValue("scanCommandConfig", out var scanCommandConfigObj) && scanCommandConfigObj is JObject scanCommandConfig)
                            {
                                var scanCommandDict = scanCommandConfig.ToObject<Dictionary<string, ScanCommandConfig>>();
                                if (scanCommandDict != null)
                                {
                                    existingConfig.ScanCommandConfig = scanCommandDict;
                                }
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

        private bool GetBooleanFromObject(object value)
        {
            if (value is bool b) return b;
            if (value is string s) return s.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (value is int i) return i != 0;
            if (value is JValue jv) return jv.Value<bool>();
            return false;
        }

        private bool HasScanTypeKeys(Dictionary<string, object> config)
        {
            return config.ContainsKey("activateSnykOpenSource") ||
                   config.ContainsKey("activateSnykCode") ||
                   config.ContainsKey("activateSnykIac");
        }

        private bool GetBoolean(Dictionary<string, object> dict, string key, bool defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (value is bool b) return b;
                if (value is string s) return s.Equals("true", StringComparison.OrdinalIgnoreCase);
                if (value is int i) return i != 0;
                if (value is JValue jv) return jv.Value<bool>();
            }
            return defaultValue;
        }

    }
}
