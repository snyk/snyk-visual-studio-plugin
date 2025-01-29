using System.Windows.Media;

namespace Snyk.VisualStudio.Extension.Theme
{
    public static class ColorExtension
    {
        public static System.Windows.Media.SolidColorBrush ToBrush(this System.Drawing.Color color)
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }
        public static System.Windows.Media.Color ToColor(this System.Windows.Media.Brush brush)
        {
            if (brush is SolidColorBrush solidColorBrush)
            {
                // Extract the color from the SolidColorBrush
                var mediaColor = solidColorBrush.Color;
                return Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
            }

            return Color.FromArgb(0, 0, 0, 0);
        }
    }
}
