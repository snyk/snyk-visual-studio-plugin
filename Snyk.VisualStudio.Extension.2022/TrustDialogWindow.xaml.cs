
namespace Snyk.VisualStudio.Extension
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Navigation;
    using Microsoft.VisualStudio.PlatformUI;

    /// <summary>
    /// Trusted dialog window for Visual Studio 2022.
    /// </summary>
    public partial class TrustDialogWindow : DialogWindow
    {
        public TrustDialogWindow(string folderPath)
        {
            this.FolderPath = folderPath;
            this.InitializeComponent();
        }

        public string FolderPath { get; }

        private void DoNotTrustButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TrustButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void TrustDialogWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}
