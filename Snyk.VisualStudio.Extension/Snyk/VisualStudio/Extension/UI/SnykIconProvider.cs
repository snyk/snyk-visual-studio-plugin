namespace Snyk.VisualStudio.Extension.UI
{
    using Snyk.VisualStudio.Extension.CLI;

    /// <summary>
    /// Provide icon path.
    /// </summary>
    public class SnykIconProvider
    {
        /// <summary>
        /// Open source security icon path.
        /// </summary>
        public const string OpenSourceSecurityIconPath = ResourcesDirectoryPath + "OpenSourceSecurity.png";

        /// <summary>
        /// SnykCode icon path.
        /// </summary>
        public const string SnykCodeIconPath = ResourcesDirectoryPath + "SnykCode.png";

        /// <summary>
        /// Default file icon path.
        /// </summary>
        public const string DefaultFileIconPath = ResourcesDirectoryPath + "DefaultFileIcon.png";

        /// <summary>
        /// Open source security icon path for dark mode.
        /// </summary>
        public const string OpenSourceSecurityDarkIconPath = ResourcesDirectoryPath + "OpenSourceSecurityDark.png";

        /// <summary>
        /// Path from this class folder to Resources folder.
        /// </summary>
        private const string ResourcesDirectoryPath = "..\\..\\..\\..\\..\\Resources\\";

        private const string NugetIconPath = ResourcesDirectoryPath + "NugetLogo.png";
        private const string NpmIconPath = ResourcesDirectoryPath + "NpmLogo.png";
        private const string JsIconPath = ResourcesDirectoryPath + "JsLogo.png";
        private const string JavaIconPath = ResourcesDirectoryPath + "JavaLogo.png";
        private const string PythonIconPath = ResourcesDirectoryPath + "PythonLogo.png";
        private const string DefaultIconPath = ResourcesDirectoryPath + "DefaultFileIcon.png";

        private const string SeverityCriticalIconPath = ResourcesDirectoryPath + "SeverityCritical.png";
        private const string SeverityHighIconPath = ResourcesDirectoryPath + "SeverityHigh.png";
        private const string SeverityMediumIconPath = ResourcesDirectoryPath + "SeverityMedium.png";
        private static readonly string SeverityLowIconPath = ResourcesDirectoryPath + "SeverityLow.png";

        /// <summary>
        /// Get package manager icon by name.
        /// </summary>
        /// <param name="packageManager">Package manager name.</param>
        /// <returns>Icon path.</returns>
        public static string GetPackageManagerIcon(string packageManager)
        {
            string iconPath = string.Empty;

            switch (packageManager)
            {
                case "nuget":
                    iconPath = NugetIconPath;
                    break;
                case "paket":
                    iconPath = NugetIconPath;
                    break;
                case "npm":
                    iconPath = NpmIconPath;
                    break;
                case "yarn":
                    iconPath = JsIconPath;
                    break;
                case "pip":
                    iconPath = PythonIconPath;
                    break;
                case "yarn-workspace":
                    iconPath = JsIconPath;
                    break;
                case "maven":
                    iconPath = JavaIconPath;
                    break;
                case "gradle":
                    iconPath = JavaIconPath;
                    break;
                default:
                    iconPath = DefaultIconPath;
                    break;
            }

            return iconPath;
        }

        /// <summary>
        /// Get icon path for severity.
        /// </summary>
        /// <param name="severity">Severity name.</param>
        /// <returns>Icon path.</returns>
        public static string GetSeverityIcon(string severity)
        {
            string icon;

            switch (severity)
            {
                case Severity.Critical:
                    icon = SeverityCriticalIconPath;

                    break;
                case Severity.High:
                    icon = SeverityHighIconPath;

                    break;
                case Severity.Medium:
                    icon = SeverityMediumIconPath;

                    break;
                case Severity.Low:
                    icon = SeverityLowIconPath;

                    break;
                default:
                    icon = DefaultIconPath;

                    break;
            }

            return icon;
        }
    }
}
