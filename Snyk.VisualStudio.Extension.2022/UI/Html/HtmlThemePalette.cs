namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// The colours <see cref="BaseHtmlProvider.ReplaceCssVariables"/> maps onto the CSS custom
    /// properties of every HTML surface, as CSS hex strings.
    /// </summary>
    /// <remarks>
    /// Holding the palette in one object keeps the mapping (here and in
    /// <c>BaseHtmlProvider</c>) separate from where the colours come from: the active Visual
    /// Studio theme, read in <c>BaseHtmlProvider.Vs.cs</c>, or <see cref="Light"/>.
    /// </remarks>
    internal sealed class HtmlThemePalette
    {
        public string BackgroundColor { get; set; }

        public string TextColor { get; set; }

        public string BorderColor { get; set; }

        public string LinkColor { get; set; }

        public string InputBackground { get; set; }

        public string InputBorder { get; set; }

        public string ButtonBackground { get; set; }

        public string ButtonHoverBackground { get; set; }

        public string DisabledForeground { get; set; }

        public string ErrorForeground { get; set; }

        public string InactiveSelectionBackground { get; set; }

        public string ListHoverBackground { get; set; }

        public string ScrollbarBackground { get; set; }

        public string ScrollbarThumb { get; set; }

        public string ScrollbarThumbHover { get; set; }

        /// <summary>
        /// Hardcoded light palette — approximates VS's Light theme so the settings
        /// dialog reads cleanly regardless of the user's active VS theme. Also the palette used
        /// wherever there is no Visual Studio theme to read.
        /// </summary>
        public static HtmlThemePalette Light => new HtmlThemePalette
        {
            BackgroundColor = "#FFFFFF",
            TextColor = "#1F1F1F",
            BorderColor = "#D4D4D4",
            LinkColor = "#0066CC",
            InputBackground = "#FFFFFF",
            InputBorder = "#CECECE",
            ButtonBackground = "#E1E1E1",
            ButtonHoverBackground = "#CECECE",
            DisabledForeground = "#A0A0A0",
            ErrorForeground = "#A1260D",
            InactiveSelectionBackground = "#E5EBF1",
            ListHoverBackground = "#F0F0F0",
            ScrollbarBackground = "#F0F0F0",
            ScrollbarThumb = "#C1C1C1",
            ScrollbarThumbHover = "#A8A8A8",
        };
    }
}
