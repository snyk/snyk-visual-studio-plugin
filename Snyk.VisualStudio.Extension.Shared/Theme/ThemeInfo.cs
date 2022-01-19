namespace Snyk.VisualStudio.Extension.Shared.Theme
{
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Contains Dark or Light theme information.
    /// </summary>
    public class ThemeInfo
    {
        private readonly string themeName;
        private readonly string themeColor;
        private readonly string themeSize;
        private readonly string themeFileName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeInfo"/> class.
        /// </summary>
        /// <param name="name">Name of theme.</param>
        /// <param name="fileName">Name of file.</param>
        /// <param name="color">Theme color.</param>
        /// <param name="size">Theme size.</param>
        public ThemeInfo(string name, string fileName, string color, string size)
        {
            this.themeName = name;
            this.themeFileName = fileName;
            this.themeColor = color;
            this.themeSize = size;
        }

        /// <summary>
        /// Gets current ThemeInfo (dark or light).
        /// </summary>
        public static ThemeInfo Current
        {
            get
            {
                var fileName = new StringBuilder(260);
                var color = new StringBuilder(260);
                var size = new StringBuilder(260);
                int currentThemeId = GetCurrentThemeName(fileName, fileName.Capacity, color, color.Capacity, size, size.Capacity);

                if (currentThemeId < 0)
                {
                    throw Marshal.GetExceptionForHR(currentThemeId);
                }

                string themeName = Path.GetFileNameWithoutExtension(fileName.ToString());

                return new ThemeInfo(themeName, fileName.ToString(), color.ToString(), size.ToString());
            }
        }

        /// <summary>
        /// Gets a theme file name.
        /// </summary>
        public string ThemeFileName => this.themeFileName;

        /// <summary>
        /// Gets a theme name.
        /// </summary>
        public string ThemeName => this.themeName;

        /// <summary>
        /// Gets a theme color.
        /// </summary>
        public string ThemeColor => this.themeColor;

        /// <summary>
        /// Gets a theme size.
        /// </summary>
        public string ThemeSize => this.themeSize;

        [DllImport("uxtheme", CharSet = CharSet.Auto)]
        private static extern int GetCurrentThemeName(
            StringBuilder pszThemeFileName,
            int dwMaxNameChars,
            StringBuilder pszColorBuff,
            int cchMaxColorChars,
            StringBuilder pszSizeBuff,
            int cchMaxSizeChars);
    }
}
