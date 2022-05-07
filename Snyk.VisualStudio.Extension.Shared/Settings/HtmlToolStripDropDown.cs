namespace Snyk.VisualStudio.Extension.Shared
{
    using System.Diagnostics;
    using System.Drawing;
    using System.Windows.Forms;

    internal class HtmlToolStripDropDown : ToolStripDropDown
    {
        private string html;

        private Size size = new Size(530, 300);

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlToolStripDropDown"/> class.
        /// </summary>
        /// <param name="html">Html text param.</param>
        public HtmlToolStripDropDown(string html)
            : base()
        {
            this.html = html;

            this.CreateWebBrowser();
            this.Initialize();
        }

        /// <summary>
        /// Gets or sets a value of host <see cref="WebBrowser"/> instance.
        /// </summary>
        public WebBrowser WebBrowser { get; set; }

        /// <summary>
        /// Show html tooltip using <see cref="System.Windows.Forms.WebBrowser"/>.
        /// </summary>
        /// <param name="labelPositionPoint">Location to show tooltip.</param>
        public void ShowTooltip(Point labelPositionPoint) => this.Show(this.WebBrowser, labelPositionPoint);

        /// <summary>
        /// Initialize HtmlToolStripDropDown control.
        /// </summary>
        public void Initialize()
        {
            this.AutoSize = false;

            var host = new ToolStripControlHost(this.WebBrowser);

            this.Margin = Padding.Empty;
            this.Padding = new Padding(2, 2, 2, 2);

            host.Margin = Padding.Empty;
            host.Padding = Padding.Empty;
            host.AutoSize = false;
            host.Size = this.WebBrowser.Size;

            this.Size = this.WebBrowser.Size;

            this.Items.Add(host);

            this.CanOverflow = true;
            this.AutoClose = true;
            this.DropShadowEnabled = true;
        }

        private void CreateWebBrowser()
        {
            this.WebBrowser = new WebBrowser();
            this.WebBrowser.DocumentText = this.html;
            this.WebBrowser.ScrollBarsEnabled = false;
            this.WebBrowser.Size = this.size;

            this.WebBrowser.Navigating += new WebBrowserNavigatingEventHandler(this.WebBrowser_Navigating);
        }

        private void WebBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            e.Cancel = true;

            Process.Start(e.Url.ToString());
        }
    }
}
