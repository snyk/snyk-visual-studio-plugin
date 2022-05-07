namespace Snyk.VisualStudio.Extension.Shared
{
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// Label with support to display html tooltip.
    /// </summary>
    public class HtmlTooltipLabel : Label
    {
        private HtmlToolStripDropDown htmlToolStripDropDown;

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlTooltipLabel"/> class.
        /// </summary>
        public HtmlTooltipLabel()
        {
            this.MouseLeave += new EventHandler((sender, eventArgs) => this.HideTooltipIfNeeded()); // Handle case to hide tooltip if mouse hover label info icon.
            this.MouseHover += new EventHandler((sender, eventArgs) => this.ShowTooltip());
        }

        /// <summary>
        /// Gets or sets a value of <see cref="UserControl"/> instance.
        /// UserControl used to properly calculate tooltip position.
        /// </summary>
        public UserControl UserControl { get; set; }

        /// <summary>
        /// Gets or sets a value of html text.
        /// </summary>
        public string Html { get; set; }

        private void ShowTooltip()
        {
            this.htmlToolStripDropDown = new HtmlToolStripDropDown(this.Html);

            var location = this.UserControl.PointToScreen(this.Location);
            location.X = location.X + 15;
            location.Y = location.Y - 5;

            this.htmlToolStripDropDown.ShowTooltip(location);

            // Handle case to hide tooltip if mouse hover tooltip object.
            this.htmlToolStripDropDown.WebBrowser.Document.MouseLeave +=
                new HtmlElementEventHandler(new HtmlElementEventHandler((sender, eventArgs) => this.HideTooltipIfNeeded()));
        }

        private void HideTooltipIfNeeded()
        {
            System.Threading.Thread.Sleep(600);

            if (this.htmlToolStripDropDown == null)
            {
                return;
            }

            var location = this.UserControl.PointToScreen(this.Location);
            var cursorPostion = Cursor.Position;

            int mouseX = cursorPostion.X;
            int mouseY = cursorPostion.Y;

            bool isXHowerTooltip = mouseX >= location.X && mouseX <= location.X + this.htmlToolStripDropDown.Width + this.Cursor.Size.Width;
            bool isYHowerTooltip = mouseY >= location.Y && mouseY <= location.Y + this.htmlToolStripDropDown.Height + this.Cursor.Size.Height;

            // Check if mouse hover tooltip text. In this case it don't hide tooltip.
            if (isXHowerTooltip && isYHowerTooltip)
            {
                return;
            }

            // If mouse don't hover tooltip object it hide tooltip.
            if (this.htmlToolStripDropDown != null)
            {
                this.htmlToolStripDropDown.Dispose();
            }

            this.htmlToolStripDropDown = null;
        }
    }
}
