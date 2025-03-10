using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Snyk.VisualStudio.Extension.Model;

namespace Snyk.VisualStudio.Extension.UI
{
    /// <summary>
    /// Provide icon path.
    /// </summary>
    public class SnykIconProvider
    {
        /// <summary>
        /// Open source security icon path.
        /// </summary>
        public static string OpenSourceSecurityIconPath = ResourceLoader.GetResourcePath("OpenSourceSecurity.png");

        /// <summary>
        /// SnykCode icon path.
        /// </summary>
        public static string SnykCodeIconPath = ResourceLoader.GetResourcePath("SnykCode.png");

        /// <summary>
        /// SnykIaC icon path.
        /// </summary>
        public static string SnykIacIconPath = ResourceLoader.GetResourcePath("SnykIac.png");

        /// <summary>
        /// Default file icon path.
        /// </summary>
        public static string DefaultFileIconPath = ResourceLoader.GetResourcePath("DefaultFileIcon.png");

        /// <summary>
        /// Open source security icon path for dark mode.
        /// </summary>
        public static string OpenSourceSecurityDarkIconPath = ResourceLoader.GetResourcePath("OpenSourceSecurityDark.png");
        /// <summary>
        /// Snyk Dog Icon.
        /// </summary>
        public static string SnykDogLogoIconPath = ResourceLoader.GetResourcePath("SnykDogLogoFullSize.png");
        public static string DarkThemeBranchIconPath = ResourceLoader.GetResourcePath("branch-dark.png");
        public static string LightThemeBranchIconPath = ResourceLoader.GetResourcePath("branch-light.png");
        private static string NugetIconPath = ResourceLoader.GetResourcePath("NugetLogo.png");
        private static string NpmIconPath = ResourceLoader.GetResourcePath("NpmLogo.png");
        private static string JsIconPath = ResourceLoader.GetResourcePath("JsLogo.png");
        private static string JavaIconPath = ResourceLoader.GetResourcePath("JavaLogo.png");
        private static string PythonIconPath = ResourceLoader.GetResourcePath("PythonLogo.png");
        private static string DefaultIconPath = ResourceLoader.GetResourcePath("DefaultFileIcon.png");
        private static string SeverityCriticalIconPath = ResourceLoader.GetResourcePath("SeverityCritical.png");
        private static string SeverityHighIconPath = ResourceLoader.GetResourcePath("SeverityHigh.png");
        private static string SeverityMediumIconPath = ResourceLoader.GetResourcePath("SeverityMedium.png");
        private static string SeverityLowIconPath = ResourceLoader.GetResourcePath("SeverityLow.png");
        private static string JsFileIconPath = ResourceLoader.GetResourcePath("JSScript.png");
        private static string CsFileIconPath = ResourceLoader.GetResourcePath("CSFileNode.png");
        private static string TsFileIconPath = ResourceLoader.GetResourcePath("TSFileNode.png");
        private static string AspFileIconPath = ResourceLoader.GetResourcePath("ASPFile.png");
        private static string CFileIconPath = ResourceLoader.GetResourcePath("CFile.png");
        private static string CppFileIconPath = ResourceLoader.GetResourcePath("CPPFileNode.png");
        private static string CppHeaderFileIconPath = ResourceLoader.GetResourcePath("CPPHeaderFile.png");
        private static string HtmlFileIconPath = ResourceLoader.GetResourcePath("HTMLFile.png");
        private static string JavaFileIconPath = ResourceLoader.GetResourcePath("JavaSource.png");
        private static string JsxFileIconPath = ResourceLoader.GetResourcePath("JSXScript.png");
        private static string PhpFileIconPath = ResourceLoader.GetResourcePath("PHPFile.png");
        private static string PythonFileIconPath = ResourceLoader.GetResourcePath("PyFileNode.png");

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
        /// Get file icon by file extension.
        /// </summary>
        /// <param name="fileExtension">File extension.</param>
        /// <returns>Icon path.</returns>
        public static string GetFileIconByExtension(string fileExtension)
        {
            string iconPath = string.Empty;

            switch (fileExtension.ToLower())
            {
                case ".h":
                case ".hpp":
                case ".hxx":
                    iconPath = CppHeaderFileIconPath;
                    break;
                case ".c":
                    iconPath = CFileIconPath;
                    break;
                case ".cc":
                case ".cpp":
                case ".cxx":
                    iconPath = CppFileIconPath;
                    break;
                case ".htm":
                case ".html":
                    iconPath = HtmlFileIconPath;
                    break;
                case ".js":
                case ".ejs":
                    iconPath = JsFileIconPath;
                    break;
                case ".jsx":
                    iconPath = JsxFileIconPath;
                    break;
                case ".cs":
                    iconPath = CsFileIconPath;
                    break;
                case ".tsx":
                case ".ts":
                    iconPath = TsFileIconPath;
                    break;
                case ".py":
                    iconPath = PythonFileIconPath;
                    break;
                case ".php":
                    iconPath = PhpFileIconPath;
                    break;
                case ".java":
                    iconPath = JavaFileIconPath;
                    break;
                case ".aspx":
                    iconPath = AspFileIconPath;
                    break;
                default:
                    iconPath = DefaultFileIconPath;
                    break;
            }

            return iconPath;
        }

        /// <summary>
        /// Get icon path for int severity.
        /// </summary>
        /// <param name="severity">Severity index.</param>
        /// <returns>Icon path.</returns>
        public static string GetSeverityIcon(int severity) => GetSeverityIcon(Severity.FromInt(severity));

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

        public static ImageSource GetImageSourceFromPath(string imagePath)
        {
            var image = ConvertPathToBitmap(imagePath);
            return image;
        }

        private static BitmapImage ConvertPathToBitmap(string path)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bitmapImage.EndInit();

            return bitmapImage;
        }
    }
}
