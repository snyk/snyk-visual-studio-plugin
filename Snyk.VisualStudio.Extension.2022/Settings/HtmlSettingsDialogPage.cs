// ABOUTME: VS Tools->Options page that hosts the WebView2-backed HtmlSettingsControl.
// ABOUTME: Wraps the control in a stable Border so VS's cached Child reference survives swaps.

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.UI.Html;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Tools→Options page that hosts the WebView2-backed <see cref="HtmlSettingsControl"/>.
    /// Inherits <see cref="UIElementDialogPage"/> so it can return a WPF element from
    /// <see cref="UIElementDialogPage.Child"/>; native VS chrome supplies the Apply/OK/Cancel
    /// buttons around it.
    /// <para>
    /// VS caches the <see cref="UIElementDialogPage.Child"/> UIElement on first access and
    /// re-uses it on every subsequent dialog open without re-querying the property. The
    /// underlying <see cref="HtmlSettingsControl"/> can't survive that round-trip — its
    /// WebView2 is destroyed by WPF's HwndHost cleanup when the dialog closes — so we wrap
    /// it in a stable <see cref="Border"/>. The Border lives forever; its child is the
    /// HtmlSettingsControl, which we swap out for a fresh instance whenever we detect the
    /// previous one has gone stale (via its <see cref="HtmlSettingsControl.IsStale"/>
    /// flag, set on Unloaded).
    /// </para>
    /// </summary>
    [Guid("8B4A3F2E-7D5C-4E11-9B3A-5F2C6D7E8A91")]
    [ComVisible(true)]
    public class HtmlSettingsDialogPage : UIElementDialogPage
    {
        private static readonly ILogger Logger = LogManager.ForContext<HtmlSettingsDialogPage>();

        private ISnykServiceProvider serviceProvider;
        private readonly Border hostBorder = new Border();
        private HtmlSettingsControl control;
        private bool visibilityHandlerWired;

        public void Initialize(ISnykServiceProvider provider)
        {
            this.serviceProvider = provider;
        }

        /// <summary>
        /// Returns the currently-active <see cref="HtmlSettingsControl"/>, creating a fresh
        /// instance if none exists or the cached one is stale. Used by <see cref="OnApply"/>
        /// to drive the page's save flow.
        /// </summary>
        public HtmlSettingsControl Control
        {
            get
            {
                EnsureFreshControl();
                return control;
            }
        }

        /// <summary>
        /// VS caches whatever UIElement we hand back here, so it has to be a long-lived
        /// instance that we can swap content into. Returning the Border means VS keeps the
        /// same wrapper across dialog opens; we replace its child with a fresh
        /// <see cref="HtmlSettingsControl"/> whenever the previous one has gone stale.
        /// </summary>
        protected override UIElement Child
        {
            get
            {
                if (!visibilityHandlerWired)
                {
                    visibilityHandlerWired = true;
                    hostBorder.IsVisibleChanged += HostBorder_IsVisibleChanged;
                }
                return hostBorder;
            }
        }

        private void HostBorder_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!hostBorder.IsVisible) return;
            EnsureFreshControl();
        }

        private void EnsureFreshControl()
        {
            // Never dispose/swap the control while a save is pumping the dispatcher (see OnApply).
            // Dispatcher.PushFrame lets visibility events through, and HostBorder_IsVisibleChanged
            // routes here — disposing the control SaveAsync is still awaiting would throw
            // ObjectDisposedException (or hang the pump). OnApply resolves the control before
            // arming this guard, so the in-flight save always has a live instance.
            if (saveInProgress) return;

            if (control != null && !control.IsStale) return;
            if (serviceProvider == null) return;

            if (control != null)
            {
                Logger.Information("[lifecycle] Swapping out stale HtmlSettingsControl {Hash}", control.GetHashCode());
                try
                {
                    control.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Failed to dispose stale HtmlSettingsControl");
                }
            }

            // Settings is single-user — only one WebView2 alive at a time — so the cached env
            // for this folder buys us nothing and leaves a stale CoreWebView2Environment
            // pointer that breaks the next open. Evict so the next show creates a fresh env.
            WebView2Host.EvictEnvironmentCache(WebView2Host.BuildUserDataFolder("settings"));

            control = new HtmlSettingsControl(serviceProvider);
            hostBorder.Child = control;
        }

        /// <summary>
        /// VS calls this when the user clicks Apply or OK in Tools→Options. We forward to
        /// <see cref="HtmlSettingsControl.SaveAsync"/> (which posts <c>getAndSaveIdeConfig()</c>
        /// into the WebView2 page and awaits the bridge's success/failure signal). On failure
        /// we set <see cref="ApplyKind.CancelNoNavigate"/> so the user stays on the Snyk page
        /// and can retry. <see cref="ThreadHelper.JoinableTaskFactory.Run"/> bridges the sync
        /// callback to the async save without deadlocking the UI thread.
        /// </summary>
        private bool saveInProgress;

        protected override void OnApply(PageApplyEventArgs e)
        {
            // Re-entrancy guard. Dispatcher.PushFrame below pumps the *full* dispatcher, so the
            // modal Options dialog can deliver a second Apply/OK click while the first save is
            // still in flight. Letting that through would re-enter SaveAsync → BeginSave and reset
            // the bridge's save-completion source mid-flight, stranding the first save (it awaits
            // the now-orphaned TCS until its 5s timeout) and racing OptionsManager.Save writes.
            // OnApply only runs on the UI thread, and the pump is the only re-entry path, so a
            // plain bool is sufficient — no locking needed. Drop the duplicate and keep the page
            // open so the in-flight save finishes undisturbed.
            if (saveInProgress)
            {
                Logger.Information("Ignoring re-entrant OnApply while a save is already in progress");
                e.ApplyBehavior = ApplyKind.CancelNoNavigate;
                return;
            }

            // Resolve (and if needed create/refresh) the control BEFORE arming saveInProgress.
            // EnsureFreshControl no-ops while saveInProgress is set, so doing this first guarantees
            // a live instance for the save; once armed, the dispatcher pump below can't dispose or
            // swap it out from under SaveAsync.
            var activeControl = Control;

            // EnsureFreshControl returns without creating a control when Initialize() hasn't run yet
            // (serviceProvider == null). Fail the apply explicitly rather than NRE'ing on SaveAsync
            // below (which the outer catch would otherwise swallow into a silent CancelNoNavigate).
            if (activeControl == null)
            {
                Logger.Warning("OnApply: no settings control available (service provider not initialized); cannot save");
                e.ApplyBehavior = ApplyKind.CancelNoNavigate;
                return;
            }

            saveInProgress = true;
            try
            {
                // We have to block OnApply until SaveAsync completes (sync VS contract),
                // but the save depends on round-tripping through WebView2's WebMessageReceived
                // event (JS posts via chrome.webview.postMessage, which lands as a dispatcher
                // message). JoinableTaskFactory.Run blocks the UI thread with a *restricted*
                // pump that only allows joinable-task continuations through — it won't
                // dispatch the WebView2 message, so the bridge never fires and the save
                // never completes. DispatcherFrame.PushFrame pumps the full dispatcher
                // including WebView2 events, which is exactly what we need here.
                // Tradeoff: pumping the full dispatcher also lets *unrelated* VS events (tool-window
                // visibility changes, timer callbacks, a second Apply/OK click) fire while we block.
                // The re-entrant Apply case is handled by the saveInProgress guard above; the rest is
                // accepted as the cost of bridging the synchronous VS apply contract to async WebView2.
                var saveSucceeded = false;
                var saveFailedWithException = false;
                var frame = new DispatcherFrame();

                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        saveSucceeded = await activeControl.SaveAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "SaveAsync threw inside OnApply pump");
                        saveFailedWithException = true;
                    }
                    finally
                    {
                        frame.Continue = false;
                    }
                }).FireAndForget();

                Dispatcher.PushFrame(frame);

                if (saveFailedWithException || !saveSucceeded)
                {
                    e.ApplyBehavior = ApplyKind.CancelNoNavigate;
                    MessageBox.Show(
                        "Failed to save Snyk settings. Check the Snyk log for details.",
                        "Snyk Settings",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                base.OnApply(e);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "OnApply failed for HtmlSettingsDialogPage");
                e.ApplyBehavior = ApplyKind.CancelNoNavigate;
            }
            finally
            {
                saveInProgress = false;
            }
        }

        /// <summary>
        /// Default <see cref="DialogPage.SaveSettingsToStorage"/> persists properties decorated
        /// with <c>[DialogPage]</c>-style attributes. We persist everything through
        /// <see cref="OnApply"/> instead, so no-op here.
        /// </summary>
        public override void SaveSettingsToStorage()
        {
        }

        /// <summary>
        /// Same reasoning as <see cref="SaveSettingsToStorage"/>. The hosted control loads
        /// its HTML on visibility change inside <see cref="HtmlSettingsControl"/>, so we
        /// don't need a separate hook here.
        /// </summary>
        public override void LoadSettingsFromStorage()
        {
        }

        protected override void OnClosed(EventArgs e)
        {
            Logger.Information("[lifecycle] HtmlSettingsDialogPage.OnClosed; control={ControlHash}",
                control?.GetHashCode() ?? 0);
            // Don't dispose here — VS may re-show the page after OnClosed (the Border stays
            // alive in VS's cache). EnsureFreshControl swaps out the stale inner control on
            // the next visibility transition.
            base.OnClosed(e);
        }
    }
}
