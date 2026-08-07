namespace Snyk.VisualStudio.Extension.UI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.VisualStudio.Imaging;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;
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

        // Bounds the wait in EnsureToolWindowExistsAsync below - must not be an unconditional await, or
        // a caller that itself blocks package init from completing would deadlock waiting on it.
        private const int PackageInitWaitTimeoutMs = 5000;

        private readonly ISnykServiceProvider serviceProvider;

        // Keyed per element, not a single shared field: Advise() returns a cookie scoped to the specific
        // element it was called on, not a global ID - a shared field would get overwritten by every
        // call, unadvising the wrong element on close. Only pruned in OnClosed, same as messagesCache
        // below; if the tool window is torn down without OnClosed firing, both outlive it for the
        // session (a missed message, not a crash or an unbounded leak).
        private readonly IDictionary<IVsInfoBarUIElement, uint> cookiesByElement =
            new Dictionary<IVsInfoBarUIElement, uint>();

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

            if (this.cookiesByElement.TryGetValue(infoBarUIElement, out var elementCookie))
            {
                infoBarUIElement.Unadvise(elementCookie);
                this.cookiesByElement.Remove(infoBarUIElement);
            }

            // FirstOrDefault returns a default KeyValuePair (Key == null) when no match is found, and
            // Dictionary<string, _>.Remove(null) throws - guard against that instead of assuming a match.
            var entry = this.messagesCache.FirstOrDefault(x => x.Value == infoBarUIElement);

            if (entry.Key != null)
            {
                this.messagesCache.Remove(entry.Key);
            }
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
        /// InfoBar is attached to it. ToolWindow stays null until the panel has been created at least
        /// once, so without this, an InfoBar shown before that point was silently dropped.
        /// </summary>
        private async Task EnsureToolWindowExistsAsync()
        {
            if (this.serviceProvider.Package.ToolWindow != null)
            {
                return;
            }

            // Wait for package init to finish before creating the pane: SnykToolWindow.OnToolWindowCreated
            // wires up SnykScanCommand.Instance with no null check, and that instance only exists once
            // SnykVSPackage.InitializeAsync reaches SnykScanCommand.InitializeAsync - which an InfoBar
            // raised earlier in that same init sequence (e.g. SnykCliDownloader.ReportInstallFailure)
            // could otherwise race, half-wiring the pane for the rest of the session. Bounded (see
            // PackageInitWaitTimeoutMs above) so a caller that itself blocks init can't deadlock here -
            // on timeout this call just skips creating the pane, same as if it had stayed uncreated.
            var packageInitialized = await Task.WhenAny(
                SnykVSPackage.PackageInitializedAwaiter,
                Task.Delay(PackageInitWaitTimeoutMs)) == SnykVSPackage.PackageInitializedAwaiter;

            if (!packageInitialized)
            {
                Logger.Warning("Timed out waiting for package initialization before showing an InfoBar; not creating the Snyk tool window this time");

                return;
            }

            try
            {
                await this.serviceProvider.Package.EnsureInitializeToolWindowAsync();
            }
            catch (Exception e)
            {
                // Best-effort: FindToolWindow/the Content cast inside EnsureInitializeToolWindowAsync
                // can throw various exceptions (e.g. during shutdown, before the package is fully sited),
                // not just NotSupportedException. The ToolWindow?.Frame == null check below still
                // catches the failure and no-ops the InfoBar rather than throwing out of this method.
                Logger.Warning(e, "Could not ensure the Snyk tool window exists before showing an InfoBar");
            }
        }

        /// <summary>
        /// Show message in infobar.
        /// </summary>
        /// <param name="message">Message.</param>
        // RunAsync + FireAndForget, not the blocking Run: this is a void method nothing awaits, and
        // EnsureToolWindowExistsAsync below can now wait up to PackageInitWaitTimeoutMs - a blocking
        // Run would tie up the caller's thread (often the UI thread) for that whole wait.
        public void ShowErrorInfoBar(string message) => ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await this.EnsureToolWindowExistsAsync();

            // Captured once, not re-read from the field: a concurrent failing
            // EnsureInitializeToolWindowAsync could otherwise null the field out between this check and
            // AddInfoBar below. Frame is checked too, not just null-ness: a pane can exist without one.
            var toolWindow = this.serviceProvider.Package.ToolWindow;

            if (toolWindow?.Frame == null || this.messagesCache.ContainsKey(message))
                return;

            var text = new InfoBarTextSpan(message);
            var submitIssueLink = new InfoBarHyperlink("Contact support", ContactSupport);
            var knownCaveatsLink = new InfoBarHyperlink("Known Caveats", KnownCaveats);

            var spans = new InfoBarTextSpan[] { text };
            var actions = new InfoBarActionItem[] { knownCaveatsLink, submitIssueLink, };
            var infoBarModel = new InfoBarModel(spans, actions, KnownMonikers.StatusError, isCloseButtonVisible: true);

            var factory = await this.serviceProvider.GetServiceAsync(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;

            var element = factory.CreateInfoBar(infoBarModel);

            element.Advise(this, out var cookie);
            this.cookiesByElement[element] = cookie;

            // Indexer, not Add: the ContainsKey check above and this line both run on the UI thread, but
            // the awaits in between can pump other messages - a second call for the same text can pass
            // its own ContainsKey check and reach this line first, in which case Add would throw on the
            // duplicate key. The indexer just overwrites, so the later call's element replaces the
            // earlier one instead of crashing.
            this.messagesCache[message] = element;

            toolWindow.AddInfoBar(element);
        }).FireAndForget();

        /// <summary>
        /// Show message in infobar.
        /// </summary>
        /// <param name="message">Message.</param>
        // See ShowErrorInfoBar's identical comment above for why this is RunAsync + FireAndForget.
        public void ShowInformationInfoBar(string message) => ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await this.EnsureToolWindowExistsAsync();

            // See ShowErrorInfoBar's identical comments above for why this is captured once and reused.
            var toolWindow = this.serviceProvider.Package.ToolWindow;

            if (toolWindow?.Frame == null || this.messagesCache.ContainsKey(message))
                return;

            var text = new InfoBarTextSpan(message);

            var spans = new InfoBarTextSpan[] { text };
            var actions = new InfoBarActionItem[]{};
            var infoBarModel = new InfoBarModel(spans, actions, KnownMonikers.StatusInformation, isCloseButtonVisible: true);

            var factory = await this.serviceProvider.GetServiceAsync(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;

            var element = factory.CreateInfoBar(infoBarModel);

            element.Advise(this, out var cookie);
            this.cookiesByElement[element] = cookie;

            // See ShowErrorInfoBar's identical comment above for why this is an indexer, not Add.
            this.messagesCache[message] = element;

            toolWindow.AddInfoBar(element);
        }).FireAndForget();
    }
}
