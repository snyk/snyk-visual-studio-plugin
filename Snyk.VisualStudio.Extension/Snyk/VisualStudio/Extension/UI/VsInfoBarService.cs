namespace Snyk.VisualStudio.Extension.UI
{
    using System;
    using Microsoft.VisualStudio.Imaging;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Snyk.VisualStudio.Extension.Service;

    /// <summary>
    /// Provide InfoBar display messages.
    /// </summary>
    public class VsInfoBarService : IVsInfoBarUIEvents
    {
        private readonly ISnykServiceProvider serviceProvider;

        private uint cookie;

        private VsInfoBarService(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public static VsInfoBarService Instance { get; private set; }

        public static void Initialize(ISnykServiceProvider serviceProvider)
        {
            Instance = new VsInfoBarService(serviceProvider);
        }

        public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
        {
            infoBarUIElement.Unadvise(this.cookie);
        }

        public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
        {
            System.Diagnostics.Process.Start("https://github.com/snyk/snyk-visual-studio-plugin/");
        }

        public async System.Threading.Tasks.Task ShowInfoBarAsync(string message)
        {
            var shell = await this.serviceProvider.GetServiceAsync(typeof(SVsShell)) as IVsShell;

            if (shell != null)
            {
                shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);

                var host = (IVsInfoBarHost)obj;

                if (host == null)
                {
                    return;
                }

                InfoBarTextSpan text = new InfoBarTextSpan(message);
                InfoBarHyperlink moreInfoLink = new InfoBarHyperlink("More information", "moreInformation");

                InfoBarTextSpan[] spans = new InfoBarTextSpan[] { text };
                InfoBarActionItem[] actions = new InfoBarActionItem[] { moreInfoLink, };
                InfoBarModel infoBarModel = new InfoBarModel(spans, actions, KnownMonikers.StatusInformation, isCloseButtonVisible: true);

                var factory = await this.serviceProvider.GetServiceAsync(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;

                IVsInfoBarUIElement element = factory.CreateInfoBar(infoBarModel);

                element.Advise(this, out cookie);

                host.AddInfoBar(element);
            }
        }
    }
}
