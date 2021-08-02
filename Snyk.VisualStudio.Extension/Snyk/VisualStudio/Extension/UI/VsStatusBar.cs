namespace Snyk.VisualStudio.Extension.UI
{
    using System.Windows;
    using EnvDTE;
    using Microsoft;
    using Microsoft.VisualStudio.Shell.Interop;
    using Snyk.VisualStudio.Extension.Service;

    public class VsStatusBar
    {
        private ISnykServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="VsStatusBar"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        private VsStatusBar(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public static VsStatusBar Instance { get; private set; }

        public static void Initialize(ISnykServiceProvider serviceProvider)
        {
            Instance = new VsStatusBar(serviceProvider);
        }

        private IVsStatusbar bar;

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

        private async System.Threading.Tasks.Task ShowMessageAsync(string message)
        {
            //await JoinableTaskFactory.SwitchToMainThreadAsync(serviceProvider.Package.DisposalToken);

            var dte = await this.serviceProvider.GetServiceAsync(typeof(DTE)) as DTE;

            Assumes.Present(dte);

            dte.StatusBar.Text = message;
        }

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
            ShowMessageAsync(message);
            //int frozen;

            //this.Bar.IsFrozen(out frozen);

            //if (frozen == 0)
            //{
            //    this.Bar.SetText(message);
            //}
        }

        public void DisplayAndShowIcon(string message)
        {
            object icon = Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_General;

            this.Bar.Animation(1, ref icon);
            this.Bar.SetText(message);

            System.Threading.Thread.Sleep(5000);

            this.Bar.Animation(0, ref icon);
            this.Bar.Clear();
        }

        public void DisplayAndShowProgress(string message)
        {
            object icon = Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_General;

            this.Bar.Animation(1, ref icon);
            this.Bar.SetText(message);

            System.Threading.Thread.Sleep(5000);

            this.Bar.Animation(0, ref icon);
            this.Bar.Clear();
        }
    }
}
