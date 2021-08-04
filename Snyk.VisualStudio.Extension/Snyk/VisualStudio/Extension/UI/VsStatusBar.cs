namespace Snyk.VisualStudio.Extension.UI
{
    using System.Windows;
    using EnvDTE;
    using Microsoft;
    using Microsoft.VisualStudio.Shell.Interop;
    using Snyk.VisualStudio.Extension.Service;

    /// <summary>
    /// Wrapper for Visual Studio status bar.
    /// </summary>
    public class VsStatusBar
    {
        private ISnykServiceProvider serviceProvider;

        private IVsStatusbar bar;

        /// <summary>
        /// Initializes a new instance of the <see cref="VsStatusBar"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        private VsStatusBar(ISnykServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

        /// <summary>
        /// Single instance of <see cref="VsStatusBar"/>.
        /// </summary>
        public static VsStatusBar Instance { get; private set; }

        /// <summary>
        /// Initialize <see cref="VsStatusBar"/>.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider implementation.</param>
        public static void Initialize(ISnykServiceProvider serviceProvider) => Instance = new VsStatusBar(serviceProvider);

        /// <summary>
        /// Gets the status bar.
        /// </summary>
        /// <value>The status bar.</value>
        protected IVsStatusbar Bar
        {
            get
            {
                if (this.bar == null)
                {
                    this.bar = this.serviceProvider.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
                }

                return this.bar;
            }
        }

        /// <summary>
        /// Show message box with title and message.
        /// </summary>
        /// <param name="title">Message box title.</param>
        /// <param name="message">Message box message.</param>
        /// <returns>Task</returns>
        public System.Threading.Tasks.Task ShowMessageBoxAsync(string title, string message)
        {
            MessageBox.Show(message, title);

            return System.Threading.Tasks.Task.CompletedTask;
        }

        /// <summary>
        /// Displays the message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void DisplayMessage(string message)
        {
            this.ShowMessageAsync(message);
        }

        private async System.Threading.Tasks.Task ShowMessageAsync(string message)
        {
            var dte = await this.serviceProvider.GetServiceAsync(typeof(DTE)) as DTE;

            Assumes.Present(dte);

            dte.StatusBar.Text = message;
        }
    }
}
