// ABOUTME: Loads HTML resources and applies Visual Studio theme colors
// ABOUTME: Replaces template placeholders with actual settings values

using System;
using System.IO;
using System.Reflection;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    public static class HtmlResourceLoader
    {
        /// <summary>
        /// Loads fallback HTML with theme colors and settings values applied.
        /// </summary>
        public static string LoadFallbackHtml(ISnykOptions options)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Snyk.VisualStudio.Extension.Resources.settings-fallback.html";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new FileNotFoundException($"Embedded resource not found: {resourceName}");

                using (var reader = new StreamReader(stream))
                {
                    var template = reader.ReadToEnd();

                    // Apply settings values
                    if (options != null)
                    {
                        // The CLI release channel can be one of the well-known values
                        // (stable / rc / preview) or any other string, in which case the
                        // form treats it as a "custom version" (e.g. "v1.1292.0").
                        var channel = options.CliReleaseChannel ?? string.Empty;
                        var isCustomChannel = channel.Length > 0
                            && channel != "stable" && channel != "rc" && channel != "preview";

                        template = template
                            .Replace("{{MANAGE_BINARIES_CHECKED}}", options.BinariesAutoUpdate ? "checked" : "")
                            .Replace("{{INSECURE_CHECKED}}", options.IgnoreUnknownCA ? "checked" : "")
                            .Replace("{{SECRETS_ENABLED_CHECKED}}", options.SecretsEnabled ? "checked" : "")
                            .Replace("{{CLI_PATH}}", options.CliCustomPath ?? "")
                            .Replace("{{CLI_BASE_DOWNLOAD_URL}}", options.CliBaseDownloadURL ?? "")
                            .Replace("{{CHANNEL_STABLE_SELECTED}}", channel == "stable" ? "selected" : "")
                            .Replace("{{CHANNEL_RC_SELECTED}}", channel == "rc" ? "selected" : "")
                            .Replace("{{CHANNEL_PREVIEW_SELECTED}}", channel == "preview" ? "selected" : "")
                            .Replace("{{CHANNEL_CUSTOM_SELECTED}}", isCustomChannel ? "selected" : "")
                            .Replace("{{CLI_RELEASE_CHANNEL_CUSTOM_VALUE}}", isCustomChannel ? channel : "")
                            .Replace("{{CLI_RELEASE_CHANNEL_CUSTOM_HIDDEN}}", isCustomChannel ? "" : "hidden");
                    }

                    // Use BaseHtmlProvider for theme replacement
                    var provider = new BaseHtmlProvider();
                    return provider.ReplaceCssVariables(template);
                }
            }
        }

        /// <summary>
        /// Applies theme colors to HTML from Language Server.
        /// </summary>
        public static string ApplyTheme(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            // Use BaseHtmlProvider for theme replacement
            var provider = new BaseHtmlProvider();
            return provider.ReplaceCssVariables(html);
        }
    }
}
