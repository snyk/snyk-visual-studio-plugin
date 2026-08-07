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

        // Bounds the wait in EnsureToolWindowExistsAsync below. Must be bounded, not awaited
        // unconditionally: SnykVSPackage.WarnIfWebView2RuntimeMissingAsync calls ShowErrorInfoBar
        // synchronously from inside InitializeAsync itself, before PackageInitializedAwaiter's task is
        // completed - an unconditional wait there deadlocks (InitializeAsync can't finish while blocked
        // on this call, and this call can't finish waiting on InitializeAsync). A few seconds is far
        // more than the narrow, no-network span this is really guarding (the gap between
        // InitializeLanguageClient's fire-and-forget start and SnykScanCommand.InitializeAsync a few
        // lines later), so this should never actually bind that legitimate wait.
        private const int PackageInitWaitTimeoutMs = 5000;

        private readonly ISnykServiceProvider serviceProvider;

        // Keyed per element, not a single shared field: Advise() returns a cookie scoped to the
        // specific element it was called on, not a global ID - a single field gets overwritten by
        // every call, so closing the first of two concurrently displayed InfoBars unadvises with
        // whatever cookie was written last, silently corrupting the other's advise connection.
        //
        // Only ever pruned in OnClosed, same as messagesCache below. If the tool window is torn down
        // (solution close, VS shutdown) without the InfoBar raising OnClosed first, both entries
        // outlive it for the rest of the session, and the same message text can never be shown again -
        // closing that would need a teardown hook (e.g. AfterCloseSolution) clearing both dictionaries.
        // Accepted as-is: the failure mode is a missed message, not a crash or a leak beyond the
        // session.
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

            // Wait for package init to finish before creating the pane, not just for the tool window's
            // own sake: SnykToolWindow.OnToolWindowCreated wires the pane's event listeners as part of
            // creation, including SnykScanCommand.Instance.UpdateControlsStateCallback with no null
            // check. SnykScanCommand.Instance is only set once SnykVSPackage.InitializeAsync reaches
            // SnykScanCommand.InitializeAsync - which runs after the fire-and-forget language-client
            // chain that can itself raise an InfoBar (e.g. SnykCliDownloader.ReportInstallFailure) and
            // land here first. Racing that chain would create the pane with only some of its listeners
            // wired, and ToolWindow's own null check above means it would stay half-wired for the rest
            // of the session.
            //
            // Bounded (see PackageInitWaitTimeoutMs above for why this must not be an unconditional
            // await): if the wait times out, package init hasn't reached the point where creating the
            // pane is safe, so this call skips creating it, same as if ToolWindow had simply stayed
            // null. A later, successful call (e.g. once the user opens the panel) still creates the
            // pane normally.
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
        public void ShowErrorInfoBar(string message) => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await this.EnsureToolWindowExistsAsync();

            // Captured once and reused below, not re-read from the field: ToolWindow can now be reset to
            // null by a concurrent failing EnsureInitializeToolWindowAsync (e.g. from another call to
            // this method), and the awaits between here and AddInfoBar below give that a window to run.
            // A stale local reference held across those awaits can't be nulled out from under us.
            var toolWindow = this.serviceProvider.Package.ToolWindow;

            // Also check Frame, not just ToolWindow: EnsureInitializeToolWindowAsync assigns ToolWindow
            // before validating its Frame, and throws (caught above, best-effort) rather than leaving
            // ToolWindow null when the pane exists but its window frame does not - so ToolWindow alone
            // being non-null does not mean AddInfoBar below has anywhere to attach to.
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

            // Indexer, not Add: the ContainsKey check above and this line both run on the UI thread
            // inside this same JoinableTaskFactory.Run, but the awaits in between can pump other
            // messages - a second call for the same text can pass its own ContainsKey check and reach
            // this line first, in which case Add would throw on the duplicate key. The indexer just
            // overwrites, so the later call's element replaces the earlier one instead of crashing.
            this.messagesCache[message] = element;

            toolWindow.AddInfoBar(element);
        });

        /// <summary>
        /// Show message in infobar.
        /// </summary>
        /// <param name="message">Message.</param>
        public void ShowInformationInfoBar(string message) => ThreadHelper.JoinableTaskFactory.Run(async () =>
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
        });
    }
}
