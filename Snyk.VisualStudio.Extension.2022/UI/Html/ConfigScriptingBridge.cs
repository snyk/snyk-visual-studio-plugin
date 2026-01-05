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
    public class ConfigScriptingBridge
    {
        private static readonly ILogger Logger = LogManager.ForContext<ConfigScriptingBridge>();
        private readonly ISnykOptions options;
        private readonly ISnykOptionsManager optionsManager;
        private readonly ISnykServiceProvider serviceProvider;
        private readonly Action onModified;
        private readonly Action onReset;
        private readonly Action<string> onAuthTokenChanged;
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
            ISnykServiceProvider serviceProvider = null,
            Action<string> onAuthTokenChanged = null)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.onModified = onModified ?? throw new ArgumentNullException(nameof(onModified));
            this.onReset = onReset;
            this.optionsManager = optionsManager ?? throw new ArgumentNullException(nameof(optionsManager));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.onAuthTokenChanged = onAuthTokenChanged;
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
            var config = JsonConvert.DeserializeObject<IdeConfigData>(jsonString);
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

        private void ApplyScanSettings(IdeConfigData config)
        {
            // Apply scan settings (activateSnykOpenSource, activateSnykCode, activateSnykIac)
            if (config.ActivateSnykOpenSource.HasValue || config.ActivateSnykCode.HasValue || config.ActivateSnykIac.HasValue)
            {
                options.OssEnabled = config.ActivateSnykOpenSource ?? false;
                options.SnykCodeSecurityEnabled = config.ActivateSnykCode ?? false;
                options.IacEnabled = config.ActivateSnykIac ?? false;
            }

            // Apply scanning mode (auto/manual)
            if (!string.IsNullOrEmpty(config.ScanningMode))
            {
                options.AutoScan = config.ScanningMode == "auto";
            }
        }

        private void ApplyIssueViewSettings(IdeConfigData config)
        {
            // Apply issue view options (issueViewOptions: {openIssues, ignoredIssues})
            if (config.IssueViewOptions != null)
            {
                options.OpenIssuesEnabled = config.IssueViewOptions.OpenIssues;
                options.IgnoredIssuesEnabled = config.IssueViewOptions.IgnoredIssues;
            }

            // Apply delta findings (enableDeltaFindings: "true"/"false" or boolean)
            if (config.EnableDeltaFindings.HasValue)
            {
                options.EnableDeltaFindings = config.EnableDeltaFindings.Value;
            }
        }

        private void ApplyAuthenticationSettings(IdeConfigData config)
        {
            // Apply authentication method (authenticationMethod: "oauth"/"token"/"pat")
            if (!string.IsNullOrEmpty(config.AuthenticationMethod))
            {
                var authMethodStr = config.AuthenticationMethod.ToLowerInvariant();
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

        private void ApplyConnectionSettings(IdeConfigData config)
        {
            if (!string.IsNullOrEmpty(config.Endpoint))
            {
                options.CustomEndpoint = config.Endpoint;
            }

            if (!string.IsNullOrEmpty(config.Token))
            {
                // Use the authenticationMethod from this request if provided, otherwise uses existing value
                // Note: Requires caller to send authenticationMethod when updating token to ensure correct pairing
                options.ApiToken = new AuthenticationToken(options.AuthenticationMethod, config.Token);
            }

            if (!string.IsNullOrEmpty(config.Organization))
            {
                options.Organization = config.Organization;
            }

            if (config.Insecure.HasValue)
            {
                options.IgnoreUnknownCA = config.Insecure.Value;
            }
        }

        private void ApplyTrustedFolders(IdeConfigData config)
        {
            if (config.TrustedFolders == null || config.TrustedFolders.Count == 0)
                return;

            var trustedFolders = new HashSet<string>();
            foreach (var folderPath in config.TrustedFolders)
            {
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

        private void ApplyCliSettings(IdeConfigData config)
        {
            if (!string.IsNullOrEmpty(config.CliPath))
            {
                options.CliCustomPath = config.CliPath;
            }

            if (config.ManageBinariesAutomatically.HasValue)
            {
                options.BinariesAutoUpdate = config.ManageBinariesAutomatically.Value;
            }

            if (!string.IsNullOrEmpty(config.BaseUrl))
            {
                options.CliDownloadUrl = config.BaseUrl;
            }

            if (!string.IsNullOrEmpty(config.CliReleaseChannel))
            {
                options.CliReleaseChannel = config.CliReleaseChannel;
            }
        }

        private void ApplyFilterSettings(IdeConfigData config)
        {
            if (config.FilterSeverity == null)
                return;

            options.FilterCritical = config.FilterSeverity.Critical;
            options.FilterHigh = config.FilterSeverity.High;
            options.FilterMedium = config.FilterSeverity.Medium;
            options.FilterLow = config.FilterSeverity.Low;
        }

        private void ApplyMiscellaneousSettings(IdeConfigData config)
        {
            // Apply risk score threshold
            options.RiskScoreThreshold = config.RiskScoreThreshold;
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
                    var additionalOptions = folderConfig.AdditionalParameters ?? string.Empty;
                    var additionalEnv = folderConfig.AdditionalEnv ?? string.Empty;
                    var preferredOrg = folderConfig.PreferredOrg ?? string.Empty;
                    var autoOrg = folderConfig.AutoDeterminedOrg ?? string.Empty;
                    var orgSetByUser = folderConfig.OrgSetByUser;

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
