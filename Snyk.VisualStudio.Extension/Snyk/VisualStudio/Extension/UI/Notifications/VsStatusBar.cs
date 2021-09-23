namespace Snyk.VisualStudio.Extension.UI
{
    using System.Threading.Tasks;
    using System.Windows;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Snyk.VisualStudio.Extension.Service;

    /// <summary>
    /// Wrapper for Visual Studio status bar.
    /// </summary>
    public class VsStatusBar
    {
        private ISnykServiceProvider serviceProvider;

        private IVsStatusbar statusBar;

        /// <summary>
        /// Initializes a new instance of the <see cref="VsStatusBar"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        private VsStatusBar(ISnykServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

        /// <summary>
        /// Gets single instance of <see cref="VsStatusBar"/>.
        /// </summary>
        public static VsStatusBar Instance { get; private set; }

        /// <summary>
        /// Initialize <see cref="VsStatusBar"/>.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider implementation.</param>
        public static void Initialize(ISnykServiceProvider serviceProvider) => Instance = new VsStatusBar(serviceProvider);

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
            _ = this.ShowMessageAsync(message);
        }

        public void ShowStartSearchMessage(string message)
        {
            _ = this.ShowMessageWithProgressIconAsync(message, (short)Constants.SBAI_Find, 1);
        }

        public void ShowFinishedSearchMessage(string message)
        {
            _ = this.ShowMessageWithProgressIconAsync(message, (short)Constants.SBAI_Find, 0);
        }

        public void ShowDownloadProgressMessage(string message)
        {
            _ = this.ShowMessageWithProgressIconAsync(message, (short)Constants.SBAI_Build, 1);
        }

        public void ShowDownloadFinishedMessage(string message)
        {
            _ = this.ShowMessageWithProgressIconAsync(message, (short)Constants.SBAI_Build, 0);
        }

        private async System.Threading.Tasks.Task ShowMessageWithProgressIconAsync(string message, object icon, int showIcon)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsStatusbar statusBar = await this.GetStatusBarAsync();

            statusBar.SetText(message);
            statusBar.Animation(showIcon, ref icon);
        }

        private async System.Threading.Tasks.Task ShowMessageAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsStatusbar statusBar = await this.GetStatusBarAsync();

            statusBar.SetText(message);
        }

        private async Task<IVsStatusbar> GetStatusBarAsync()
        {
            if (this.statusBar == null)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.statusBar = (IVsStatusbar)await this.serviceProvider.GetServiceAsync(typeof(SVsStatusbar));
            }

            return this.statusBar;
        }
    }
}
