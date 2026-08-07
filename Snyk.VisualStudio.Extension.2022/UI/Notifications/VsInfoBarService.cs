namespace Snyk.VisualStudio.Extension.UI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.VisualStudio.Imaging;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Serilog;
    using Snyk.VisualStudio.Extension.Service;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Provide InfoBar display messages.
    /// </summary>
    public class VsInfoBarService : IVsInfoBarUIEvents
    {
        private static readonly ILogger Logger = LogManager.ForContext<VsInfoBarService>();

        private const string ContactSupport = "contactSupport";
        private const string KnownCaveats = "knownCaveats";
        private const string SupportLink = "https://support.snyk.io/hc/en-us/requests/new";
        private const string KnownCaveatsLink = "https://docs.snyk.io/ide-tools/visual-studio-extension/troubleshooting-and-known-issues-with-visual-studio-extension";
        private readonly ISnykServiceProvider serviceProvider;

        private uint cookie;

        /// <summary>
        /// Cache/save all displayed messages for prevent display same message multiple times.
        /// </summary>
        private readonly IDictionary<string, IVsInfoBarUIElement> messagesCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="VsInfoBarService"/> class.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider.</param>
        public VsInfoBarService(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            this.messagesCache = new Dictionary<string, IVsInfoBarUIElement>();
        }

        /// <summary>
        /// Handle on close event.
        /// </summary>
        /// <param name="infoBarUIElement">Info bar UI element object.</param>
        public void OnClosed(IVsInfoBarUIElement infoBarUIElement) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            infoBarUIElement.Unadvise(this.cookie);

            this.messagesCache.Remove(this.messagesCache.FirstOrDefault(x => x.Value == infoBarUIElement).Key);
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

                if (ContactSupport == actionItem.ActionContext.ToString())
                {
                    Process.Start(SupportLink);
                }

                if (KnownCaveats == actionItem.ActionContext.ToString())
                {
                    Process.Start(KnownCaveatsLink);
                }

                return Task.CompletedTask;
            });

        /// <summary>
        /// Ensures the Snyk tool window pane exists (without necessarily showing/focusing it) before an
        /// InfoBar is attached to it (IDE-2454). ToolWindow stays null until the panel has been created
        /// at least once - opened by the user, or by a Snyk command running EnsureInitializeToolWindowAsync
        /// - so without this, any InfoBar shown before that point (e.g. a startup warning or a
        /// pre-launch gate failure, before the user has ever opened the panel) was silently dropped: the
        /// early-return check below never became true, and no message reached the user at all.
        /// </summary>
        private async Task EnsureToolWindowExistsAsync()
        {
            if (this.serviceProvider.Package.ToolWindow != null)
            {
                return;
            }

            try
            {
                await this.serviceProvider.Package.EnsureInitializeToolWindowAsync();
            }
            catch (Exception e)
            {
                // Best-effort: the ToolWindow == null check below still applies and no-ops exactly as it
                // did before this fix if this failed (e.g. during shutdown, before the package is fully
                // sited - FindToolWindow/the Content cast inside EnsureInitializeToolWindowAsync can
                // throw various exceptions in those cases, not just NotSupportedException).
                Logger.Warning(e, "Could not ensure the Snyk tool window exists before showing an InfoBar");
            }
        }

        /// <summary>
        /// Show message in infobar.
        /// </summary>
        /// <param name="message">Message.</param>
        public void ShowErrorInfoBar(string message) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await this.EnsureToolWindowExistsAsync();

            if (this.serviceProvider.Package.ToolWindow == null || this.messagesCache.ContainsKey(message))
                return;

            var text = new InfoBarTextSpan(message);
            var submitIssueLink = new InfoBarHyperlink("Contact support", ContactSupport);
            var knownCaveatsLink = new InfoBarHyperlink("Known Caveats", KnownCaveats);

            var spans = new InfoBarTextSpan[] { text };
            var actions = new InfoBarActionItem[] { knownCaveatsLink, submitIssueLink, };
            var infoBarModel = new InfoBarModel(spans, actions, KnownMonikers.StatusError, isCloseButtonVisible: true);

            var factory = await this.serviceProvider.GetServiceAsync(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;

            var element = factory.CreateInfoBar(infoBarModel);

            element.Advise(this, out this.cookie);

            this.messagesCache.Add(message, element);
            
            this.serviceProvider.Package.ToolWindow.AddInfoBar(element);
        });

        /// <summary>
        /// Show message in infobar.
        /// </summary>
        /// <param name="message">Message.</param>
        public void ShowInformationInfoBar(string message) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await this.EnsureToolWindowExistsAsync();

            if (this.serviceProvider.Package.ToolWindow == null || this.messagesCache.ContainsKey(message))
                return;

            var text = new InfoBarTextSpan(message);

            var spans = new InfoBarTextSpan[] { text };
            var actions = new InfoBarActionItem[]{};
            var infoBarModel = new InfoBarModel(spans, actions, KnownMonikers.StatusInformation, isCloseButtonVisible: true);

            var factory = await this.serviceProvider.GetServiceAsync(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;

            var element = factory.CreateInfoBar(infoBarModel);

            element.Advise(this, out this.cookie);

            this.messagesCache.Add(message, element);

            this.serviceProvider.Package.ToolWindow.AddInfoBar(element);
        });
    }
}
