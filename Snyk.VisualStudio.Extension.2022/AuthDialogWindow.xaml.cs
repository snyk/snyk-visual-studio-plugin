using System.Threading.Tasks;
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

        public AuthDialogWindow()
        {
            this.InitializeComponent();
            this.Closing += AuthDialogWindow_Closing;
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
