namespace Snyk.VisualStudio.Extension.UI.Html
{
    public static class ColorExtension
    {
        public static string ToHex(this System.Drawing.Color c)
            => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        public static string ToHex(this System.Windows.Media.Color c)
            => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private static string ToRGB(this System.Drawing.Color c)
            => $"rgb({c.R},{c.G},{c.B}, {c.A})";
    }
}