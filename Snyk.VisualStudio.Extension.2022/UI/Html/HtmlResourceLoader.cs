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
                        template = template
                            .Replace("{{MANAGE_BINARIES_CHECKED}}", options.BinariesAutoUpdate ? "checked" : "")
                            .Replace("{{CLI_PATH}}", options.CliCustomPath ?? "")
                            .Replace("{{CLI_BASE_DOWNLOAD_URL}}", options.CliDownloadUrl ?? "")
                            .Replace("{{CHANNEL_STABLE_SELECTED}}", options.CliReleaseChannel == "stable" ? "selected" : "")
                            .Replace("{{CHANNEL_RC_SELECTED}}", options.CliReleaseChannel == "rc" ? "selected" : "")
                            .Replace("{{CHANNEL_PREVIEW_SELECTED}}", options.CliReleaseChannel == "preview" ? "selected" : "");
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
