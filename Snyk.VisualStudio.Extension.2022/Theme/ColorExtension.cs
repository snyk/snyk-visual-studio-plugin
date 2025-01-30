using System.Windows.Media;

namespace Snyk.VisualStudio.Extension.Theme
{
    public static class ColorExtension
    {
        public static System.Windows.Media.SolidColorBrush ToBrush(this System.Drawing.Color color)
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }
    }
}
