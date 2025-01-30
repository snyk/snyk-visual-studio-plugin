using System.Windows.Media;

namespace Snyk.VisualStudio.Extension.Theme
{
    public static class ColorExtension
    {
        public static SolidColorBrush ToBrush(this System.Drawing.Color color)
        {
            return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        }
    }
}
