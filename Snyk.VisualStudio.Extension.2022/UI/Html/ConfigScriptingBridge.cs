// ABOUTME: JavaScript-to-C# bridge for WebBrowser control using ObjectForScripting
// ABOUTME: Handles configuration saves and state changes from HTML form

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Snyk.VisualStudio.Extension.Authentication;
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
        private readonly Action onModified;
        private readonly Action onReset;
        private readonly Action onSaveComplete;

        public ConfigScriptingBridge(
            ISnykOptions options,
            Action onModified,
            Action onReset = null,
            Action onSaveComplete = null)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.onModified = onModified ?? throw new ArgumentNullException(nameof(onModified));
            this.onReset = onReset;
            this.onSaveComplete = onSaveComplete;
        }

        /// <summary>
        /// Called from JavaScript: window.external.SaveIdeConfig(jsonString)
        /// </summary>
        public void SaveIdeConfig(string jsonString)
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
        /// Called from JavaScript: window.external.OnFormDirtyChange(isDirty)
        /// </summary>
        public void OnFormDirtyChange(bool isDirty)
        {
            if (isDirty)
                onModified?.Invoke();
            else
                onReset?.Invoke();
        }

        /// <summary>
        /// Called from JavaScript: window.external.Login()
        /// </summary>
        public void Login()
        {
            // TODO: Trigger authentication flow
            System.Diagnostics.Debug.WriteLine("Login requested from HTML");
        }

        /// <summary>
        /// Called from JavaScript: window.external.Logout()
        /// </summary>
        public void Logout()
        {
            options.ApiToken = AuthenticationToken.EmptyToken;
            // TODO: Notify LS of logout
            System.Diagnostics.Debug.WriteLine("Logout requested from HTML");
        }

        private void ParseAndSaveConfig(string jsonString)
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (config == null) return;

            // Apply scan settings (only if keys present)
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
                    // Determine token type - check if it's a PAT (starts with snyk_)
                    var tokenType = tokenString.StartsWith("snyk_")
                        ? AuthenticationType.Pat
                        : AuthenticationType.Token;
                    options.ApiToken = new AuthenticationToken(tokenType, tokenString);
                }
            }
            if (config.ContainsKey("insecure"))
                options.IgnoreUnknownCA = GetBoolean(config, "insecure", false);

            // Note: Severity filters are not in IPersistableOptions
            // These would need to be added to the interface if needed
            // For now, commenting out:
            // if (config.TryGetValue("filterSeverity", out var severity) && severity is JObject severityObj)
            // {
            //     var severityDict = severityObj.ToObject<Dictionary<string, object>>();
            //     if (severityDict != null)
            //     {
            //         options.CriticalSeverityEnabled = GetBoolean(severityDict, "critical", true);
            //         options.HighSeverityEnabled = GetBoolean(severityDict, "high", true);
            //         options.MediumSeverityEnabled = GetBoolean(severityDict, "medium", true);
            //         options.LowSeverityEnabled = GetBoolean(severityDict, "low", true);
            //     }
            // }

            // Apply CLI settings (using actual ISnykOptions property names)
            if (config.TryGetValue("cliPath", out var cliPath))
                options.CliCustomPath = cliPath?.ToString();
            if (config.ContainsKey("manageBinariesAutomatically"))
                options.BinariesAutoUpdate = GetBoolean(config, "manageBinariesAutomatically", true);
            if (config.TryGetValue("cliBaseDownloadURL", out var baseUrl) && !string.IsNullOrEmpty(baseUrl?.ToString()))
                options.CliDownloadUrl = baseUrl.ToString();
            if (config.TryGetValue("cliReleaseChannel", out var channel) && !string.IsNullOrEmpty(channel?.ToString()))
                options.CliReleaseChannel = channel.ToString();
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
