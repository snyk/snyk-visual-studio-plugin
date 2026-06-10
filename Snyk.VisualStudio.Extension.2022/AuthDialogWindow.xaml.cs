using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension
{
    public partial class AuthDialogWindow : DialogWindow
    {
        public static AuthDialogWindow Instance { get; } = new AuthDialogWindow();

        // Guards the show/hide race in the auth flow: login runs fire-and-forget while the modal
        // ShowDialog() is started separately, so a fast success/failure can call Hide() *before*
        // ShowDialog() runs. A plain Hide() then no-ops and the dialog shows stuck. ArmForShow()
        // resets this at the start of an attempt; HideForAuthResult() records that the result has
        // arrived; ShowDialogForAuth() skips the show if it already has. Touched only on the UI
        // thread (all callers SwitchToMainThread first); volatile guards the ArmForShow write that
        // may originate off the UI thread.
        private volatile bool authResultArrived;

        public AuthDialogWindow()
        {
            this.InitializeComponent();
            this.Closing += AuthDialogWindow_Closing;
        }

        /// <summary>Resets the show/hide guard at the start of an authentication attempt.</summary>
        public void ArmForShow() => this.authResultArrived = false;

        /// <summary>
        /// Shows the modal auth dialog, unless the auth result already arrived (and hid it) before
        /// we got here — which would otherwise leave a dialog on screen that nothing will close.
        /// </summary>
        public void ShowDialogForAuth()
        {
            if (this.authResultArrived)
                return;
            this.ShowDialog();
        }

        /// <summary>
        /// Hides the dialog because authentication completed (success or failure). If the modal
        /// show hasn't run yet, this also suppresses it via the <see cref="ArmForShow"/> guard.
        /// </summary>
        public void HideForAuthResult()
        {
            this.authResultArrived = true;
            this.Visibility = Visibility.Hidden;
        }

        private void AuthDialogWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }

        private void AuthDialogWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CopyLinkButton_OnClick(object sender, RoutedEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () => await LanguageClientHelper.LanguageClientManager().InvokeCopyLinkAsync(SnykVSPackage.Instance.DisposalToken)).FireAndForget();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
