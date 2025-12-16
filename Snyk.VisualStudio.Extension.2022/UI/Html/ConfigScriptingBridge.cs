// ABOUTME: JavaScript-to-C# bridge for WebBrowser control using ObjectForScripting
// ABOUTME: Handles configuration saves and state changes from HTML form

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Snyk.VisualStudio.Extension;
using Snyk.VisualStudio.Extension.Authentication;
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
        private readonly ISnykOptions options;
        private readonly ISnykOptionsManager optionsManager;
        private readonly ISnykServiceProvider serviceProvider;
        private readonly Action onModified;
        private readonly Action onReset;
        private readonly Action onSaveComplete;

        public ConfigScriptingBridge(
            ISnykOptions options,
            Action onModified,
            Action onReset = null,
            Action onSaveComplete = null,
            ISnykOptionsManager optionsManager = null,
            ISnykServiceProvider serviceProvider = null)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.onModified = onModified ?? throw new ArgumentNullException(nameof(onModified));
            this.onReset = onReset;
            this.onSaveComplete = onSaveComplete;
            this.optionsManager = optionsManager;
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Called from LS HTML JavaScript: window.__saveIdeConfig__(jsonString)
        /// The LS HTML handles all validation and data collection - we just save the config.
        /// </summary>
        public void __saveIdeConfig__(string jsonString)
        {
            try
            {
                ParseAndSaveConfig(jsonString);
                onSaveComplete?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex}");
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
                    System.Diagnostics.Debug.WriteLine("Authentication initiated from HTML");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Cannot authenticate: GeneralOptionsDialogPage not available");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during login: {ex.Message}");
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
                        System.Diagnostics.Debug.WriteLine("Logout completed - Language Server notified");
                    }).FireAndForget();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Logout completed - Language Server not available");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during logout: {ex.Message}");
            }
        }

        /// <summary>
        /// Called from LS HTML JavaScript: window.__ideSaveAttemptFinished__(status)
        /// Optional callback to track save attempt results.
        /// </summary>
        public void __ideSaveAttemptFinished__(string status)
        {
            System.Diagnostics.Debug.WriteLine($"Save attempt finished with status: {status}");
            // Can be used for telemetry or UI feedback
        }

        private void ParseAndSaveConfig(string jsonString)
        {
            // LS HTML JavaScript handles all validation - we just parse and save
            var config = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (config == null) return;

            // Apply scan settings
            if (HasScanTypeKeys(config))
            {
                options.OssEnabled = GetBoolean(config, "activateSnykOpenSource", false);
                options.SnykCodeSecurityEnabled = GetBoolean(config, "activateSnykCode", false);
                options.IacEnabled = GetBoolean(config, "activateSnykIac", false);
            }

            // Apply scanning mode
            if (config.TryGetValue("scanningMode", out var mode))
            {
                options.AutoScan = mode?.ToString() == "auto";
            }

            // Apply connection settings
            if (config.TryGetValue("organization", out var org))
                options.Organization = org?.ToString();
            if (config.TryGetValue("endpoint", out var endpoint))
                options.CustomEndpoint = endpoint?.ToString();
            if (config.TryGetValue("token", out var token))
            {
                var tokenString = token?.ToString();
                if (!string.IsNullOrEmpty(tokenString))
                {
                    var tokenType = tokenString.StartsWith("snyk_")
                        ? AuthenticationType.Pat
                        : AuthenticationType.Token;
                    options.ApiToken = new AuthenticationToken(tokenType, tokenString);
                }
            }
            if (config.ContainsKey("insecure"))
                options.IgnoreUnknownCA = GetBoolean(config, "insecure", false);

            // Apply CLI settings
            if (config.TryGetValue("cliPath", out var cliPath))
                options.CliCustomPath = cliPath?.ToString();
            if (config.ContainsKey("manageBinariesAutomatically"))
                options.BinariesAutoUpdate = GetBoolean(config, "manageBinariesAutomatically", true);
            if (config.TryGetValue("cliBaseDownloadURL", out var baseUrl) && !string.IsNullOrEmpty(baseUrl?.ToString()))
                options.CliDownloadUrl = baseUrl.ToString();
            if (config.TryGetValue("cliReleaseChannel", out var channel) && !string.IsNullOrEmpty(channel?.ToString()))
                options.CliReleaseChannel = channel.ToString();

            // Per-solution/folder settings (handled by LS HTML)
            // The LS HTML sends folder configurations in the JSON - we need to save them to optionsManager
            if (config.TryGetValue("folderConfigs", out var folderConfigsObj) && folderConfigsObj is JArray folderConfigs && optionsManager != null)
            {
                SaveFolderConfigs(folderConfigs);
            }
        }

        private void SaveFolderConfigs(JArray folderConfigs)
        {
            // LS HTML sends folder configs for the current solution
            // We save using the current solution's hash (matching existing SolutionSettings pattern)
            if (folderConfigs == null || folderConfigs.Count == 0)
                return;

            try
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    foreach (var folderConfigToken in folderConfigs)
                    {
                        var folderConfig = folderConfigToken.ToObject<Dictionary<string, object>>();
                        if (folderConfig == null) continue;

                        // Extract folder settings
                        var additionalOptions = folderConfig.TryGetValue("additionalOptions", out var addOpts)
                            ? addOpts?.ToString() ?? string.Empty
                            : string.Empty;

                        var preferredOrg = folderConfig.TryGetValue("preferredOrg", out var prefOrg)
                            ? prefOrg?.ToString() ?? string.Empty
                            : string.Empty;

                        var autoOrg = folderConfig.TryGetValue("autoOrg", out var autOrg)
                            ? autOrg?.ToString() ?? string.Empty
                            : string.Empty;

                        var orgSetByUser = folderConfig.TryGetValue("orgSetByUser", out var orgSetByUserObj)
                            ? GetBooleanFromObject(orgSetByUserObj)
                            : false;

                        // Save to current solution using SnykOptionsManager
                        // (matches pattern from SnykSolutionOptionsUserControl)
                        await optionsManager.SaveAdditionalOptionsAsync(additionalOptions);
                        await optionsManager.SavePreferredOrgAsync(preferredOrg);
                        await optionsManager.SaveAutoDeterminedOrgAsync(autoOrg);
                        await optionsManager.SaveOrgSetByUserAsync(orgSetByUser);

                        System.Diagnostics.Debug.WriteLine($"Saved folder config - AdditionalOptions: {additionalOptions}, OrgSetByUser: {orgSetByUser}");
                    }
                }).FireAndForget();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving folder configs: {ex}");
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
