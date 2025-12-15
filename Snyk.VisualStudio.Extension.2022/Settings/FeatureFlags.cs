// ABOUTME: Feature flag management for experimental features
// ABOUTME: Controls enablement of HTML configuration dialog

using Microsoft.Win32;
using System;

namespace Snyk.VisualStudio.Extension.Settings
{
    public static class FeatureFlags
    {
        private const string RegistryPath = @"Software\Snyk\VisualStudio";

        /// <summary>
        /// Returns true if the new HTML-based configuration dialog should be used.
        /// Controlled by registry key: HKCU\Software\Snyk\VisualStudio\UseHtmlConfigDialog
        ///
        /// Default: ENABLED (true)
        /// To disable: Set-ItemProperty -Path "HKCU:\Software\Snyk\VisualStudio" -Name "UseHtmlConfigDialog" -Value 0
        /// To re-enable: Set-ItemProperty -Path "HKCU:\Software\Snyk\VisualStudio" -Name "UseHtmlConfigDialog" -Value 1
        /// </summary>
        public static bool UseHtmlConfigDialog
        {
            get
            {
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                    {
                        var value = key?.GetValue("UseHtmlConfigDialog");
                        if (value is int intValue)
                            return intValue != 0;
                        if (value is string strValue)
                            return strValue.Equals("true", StringComparison.OrdinalIgnoreCase);
                        return true; // Default: ENABLED
                    }
                }
                catch
                {
                    return true; // Default: ENABLED on error
                }
            }
        }
    }
}
