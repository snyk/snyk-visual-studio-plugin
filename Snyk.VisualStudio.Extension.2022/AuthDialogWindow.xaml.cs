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
        // ShowDialog() is started separately, so a fast success/failure can call HideForAuthResult()
        // *before* ShowDialogForAuth() runs. ArmForShow() resets this at the start of an attempt;
        // HideForAuthResult() records that the result arrived; ShowDialogForAuth() skips the show if
        // it already has. HideForAuthResult/ShowDialogForAuth touch UI state and run on the UI thread
        // (their callers SwitchToMainThread first), but ArmForShow() is invoked synchronously from
        // Authenticate(), which can run off the UI thread — hence volatile, so the flag write is
        // visible to the UI-thread reads.
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
        /// Also no-ops if the dialog is already visible, so a re-entrant auth attempt doesn't hit
        /// <see cref="System.Windows.Window.ShowDialog"/> on a shown window (which throws
        /// InvalidOperationException). UI-thread only.
        /// </summary>
        public void ShowDialogForAuth()
        {
            if (this.authResultArrived || this.IsVisible)
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
            // Visibility.Hidden (not Close/DialogResult) because this window is a process-wide
            // singleton, reused across auth attempts. Auth is fire and forget, so it's OK that
            // ShowDialog() keeps going.
            this.Visibility = Visibility.Hidden;
        }

        private void AuthDialogWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Keep the singleton alive: cancel the close (X button / Cancel) and hide instead, so the
            // window can be re-shown on the next auth attempt. 
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
