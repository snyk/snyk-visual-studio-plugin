// ABOUTME: Loads HTML resources and applies Visual Studio theme colors
// ABOUTME: Replaces template placeholders with actual settings values

using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    public static class HtmlResourceLoader
    {
        /// <summary>
        /// Loads fallback HTML with theme colors and settings values applied.
        /// Pass <paramref name="forceLight"/> to render in light mode regardless of the
        /// active VS theme — used by the settings dialog.
        /// </summary>
        public static string LoadFallbackHtml(ISnykOptions options, bool forceLight = false)
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
                    var provider = new BaseHtmlProvider(forceLight);
                    return provider.ReplaceCssVariables(template);
                }
            }
        }

        /// <summary>
        /// Applies theme colors to HTML from Language Server. Pass <paramref name="forceLight"/>
        /// to render in light mode regardless of the active VS theme — used by the settings dialog.
        /// </summary>
        public static string ApplyTheme(string html, bool forceLight = false)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            // Use BaseHtmlProvider for theme replacement
            var provider = new BaseHtmlProvider(forceLight);
            html = provider.ReplaceCssVariables(html);

            if (forceLight)
            {
                html = InjectDisabledButtonOverride(html);
            }

            return html;
        }

        // The LS settings stylesheet's disabled-button rule only swaps the background
        // (to --button-secondary-background) and leaves the text at --button-foreground.
        // In the forced-light settings dialog that resolves to white text on a near-white
        // background, so a disabled primary button (e.g. "Authenticate" once signed in) is
        // unreadable. Inject a settings-only override giving disabled buttons a conventional
        // muted-grey appearance. Reuses the document's existing CSP nonce because the page's
        // style-src policy only allows nonce'd inline styles.
        private static string InjectDisabledButtonOverride(string html)
        {
            var nonceMatch = Regex.Match(html, "nonce=\"(?<nonce>[^\"]+)\"");
            var nonceAttr = nonceMatch.Success ? $" nonce=\"{nonceMatch.Groups["nonce"].Value}\"" : string.Empty;

            var styleBlock =
                $"<style{nonceAttr}>" +
                "button:disabled,button[disabled]," +
                "button.secondary:disabled,button.secondary[disabled]{" +
                "background-color:#E1E1E1 !important;" +
                "color:#6E6E6E !important;" +
                "opacity:1 !important;}" +
                "</style>";

            // Insert at the very end of the body so the override comes after every other
            // stylesheet in the document and wins the cascade (equal specificity + !important).
            var bodyCloseIndex = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (bodyCloseIndex >= 0)
            {
                return html.Insert(bodyCloseIndex, styleBlock);
            }

            var headCloseIndex = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
            if (headCloseIndex >= 0)
            {
                return html.Insert(headCloseIndex, styleBlock);
            }

            // No <head>/<body> (e.g. a fragment) — append so the override still loads last.
            return html + styleBlock;
        }
    }
}
