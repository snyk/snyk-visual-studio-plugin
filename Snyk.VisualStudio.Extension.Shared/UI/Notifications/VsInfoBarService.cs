namespace Snyk.VisualStudio.Extension.Shared.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.Imaging;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Snyk.VisualStudio.Extension.Shared.Service;

    /// <summary>
    /// Provide InfoBar display messages.
    /// </summary>
    public class VsInfoBarService : IVsInfoBarUIEvents
    {
        private readonly ISnykServiceProvider serviceProvider;

        private uint cookie;

        private IDictionary<string, IVsInfoBarUIElement> messagesDictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="VsInfoBarService"/> class.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider.</param>
        public VsInfoBarService(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            this.messagesDictionary = new Dictionary<string, IVsInfoBarUIElement>();
        }

        /// <summary>
        /// Handle on close event.
        /// </summary>
        /// <param name="infoBarUIElement">Info bar UI element object.</param>
        public void OnClosed(IVsInfoBarUIElement infoBarUIElement) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            infoBarUIElement.Unadvise(this.cookie);

            this.messagesDictionary.Remove(this.messagesDictionary.FirstOrDefault(x => x.Value == infoBarUIElement).Key);
        });

        /// <summary>
        /// On Action item cliecked handler.
        /// </summary>
        /// <param name="infoBarUIElement">UI element object.</param>
        /// <param name="actionItem">Action item.</param>
        public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
            => ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (actionItem.ActionContext == "submitIssue")
                {
                    System.Diagnostics.Process.Start("https://github.com/snyk/snyk-visual-studio-plugin/issues");
                }

                if (actionItem.ActionContext == "knownCaveats")
                {
                    System.Diagnostics.Process.Start("https://github.com/snyk/snyk-visual-studio-plugin#known-caveats");
                }

                return System.Threading.Tasks.Task.CompletedTask;
            });

        /// <summary>
        /// Show message in infobar.
        /// </summary>
        /// <param name="message">Message.</param>
        public void ShowErrorInfoBar(string message) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (this.messagesDictionary.ContainsKey(message))
            {
                return;
            }

            var text = new InfoBarTextSpan(message);
            var submitIssueLink = new InfoBarHyperlink("Submit an issue", "submitIssue");
            var knownCaveatsLink = new InfoBarHyperlink("Known Caveats", "knownCaveats");

            var spans = new InfoBarTextSpan[] { text };
            var actions = new InfoBarActionItem[] { knownCaveatsLink, submitIssueLink, };
            var infoBarModel = new InfoBarModel(spans, actions, KnownMonikers.StatusError, isCloseButtonVisible: true);

            var factory = await this.serviceProvider.GetServiceAsync(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;

            var element = factory.CreateInfoBar(infoBarModel);

            element.Advise(this, out this.cookie);

            this.messagesDictionary.Add(message, element);

            this.serviceProvider.Package.ToolWindow.AddInfoBar(element);
        });
    }
}
