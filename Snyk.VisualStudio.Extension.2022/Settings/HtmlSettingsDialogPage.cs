// ABOUTME: VS Tools->Options page that hosts the WebView2-backed HtmlSettingsControl.
// ABOUTME: Bridges VS DialogPage sync lifecycle (OnApply) to the async control.SaveAsync.

using System;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Tools→Options page that hosts the WebView2-backed <see cref="HtmlSettingsControl"/>.
    /// Inherits <see cref="UIElementDialogPage"/> so it can return a WPF element from
    /// <see cref="UIElementDialogPage.Child"/>; native VS chrome supplies the Apply/OK/Cancel
    /// buttons around it.
    /// </summary>
    [Guid("8B4A3F2E-7D5C-4E11-9B3A-5F2C6D7E8A91")]
    [ComVisible(true)]
    public class HtmlSettingsDialogPage : UIElementDialogPage
    {
        private static readonly ILogger Logger = LogManager.ForContext<HtmlSettingsDialogPage>();

        private ISnykServiceProvider serviceProvider;
        private HtmlSettingsControl control;

        public void Initialize(ISnykServiceProvider provider)
        {
            this.serviceProvider = provider;
        }

        /// <summary>
        /// Lazily constructed on first access — Tools→Options instantiates the DialogPage
        /// long before the user clicks on the Snyk node, so we don't want to spin up a
        /// WebView2 environment until it's actually needed.
        /// </summary>
        public HtmlSettingsControl Control
        {
            get
            {
                if (control == null)
                {
                    if (serviceProvider == null)
                        throw new InvalidOperationException(
                            $"{nameof(HtmlSettingsDialogPage)} accessed before Initialize was called.");
                    control = new HtmlSettingsControl(serviceProvider);
                }
                return control;
            }
        }

        protected override UIElement Child => Control;

        /// <summary>
        /// VS calls this when the user clicks Apply or OK in Tools→Options. We forward to
        /// <see cref="HtmlSettingsControl.SaveAsync"/> (which posts <c>getAndSaveIdeConfig()</c>
        /// into the WebView2 page and awaits the bridge's success/failure signal). On failure
        /// we set <see cref="ApplyKind.CancelNoNavigate"/> so the user stays on the Snyk page
        /// and can retry. <see cref="ThreadHelper.JoinableTaskFactory.Run"/> bridges the sync
        /// callback to the async save without deadlocking the UI thread.
        /// </summary>
        protected override void OnApply(PageApplyEventArgs e)
        {
            try
            {
                var saveSucceeded = ThreadHelper.JoinableTaskFactory.Run(async () =>
                    await Control.SaveAsync());

                if (!saveSucceeded)
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
        /// Same reasoning as <see cref="SaveSettingsToStorage"/>. The hosted
        /// <see cref="HtmlSettingsControl"/> loads its HTML in its own Loaded handler, which
        /// fires whenever the control re-enters the visual tree (i.e. each Tools→Options open),
        /// so we don't need a separate hook here.
        /// </summary>
        public override void LoadSettingsFromStorage()
        {
        }
    }
}
