using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension
{
    public partial class BranchSelectorDialogWindow : DialogWindow
    {
        private readonly ISnykServiceProvider serviceProvider;
        public FolderConfig FolderConfig { get; set; }
        public static bool IsOpen { get; set; }
        
        public BranchSelectorDialogWindow(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.InitializeComponent();

            var currentFolder = ThreadHelper.JoinableTaskFactory.Run(async () =>
                await this.serviceProvider.SolutionService.GetSolutionFolderAsync()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            FolderConfig =
                this.serviceProvider.Options?.FolderConfigs?.SingleOrDefault(x => x.FolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) == currentFolder);
            if (FolderConfig == null)
                return;
            LblFolderPathForBranch.Text = FolderConfig.FolderPath;
            LblFolderPathForReferenceFolder.Text = FolderConfig.FolderPath;
            CbBranchList.ItemsSource = FolderConfig.LocalBranches;
            CbBranchList.SelectedItem = FolderConfig.BaseBranch;
            SelectedFolderPath.Text = FolderConfig.ReferenceFolderPath;
            IsOpen = true;
        }

        private void BranchSelectorDialogWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (FolderConfig == null)
            {
                return;
            }
            FolderConfig.BaseBranch = CbBranchList.SelectedItem.ToString();
            FolderConfig.ReferenceFolderPath = SelectedFolderPath.Text;

            var folderConfigList = this.serviceProvider.Options.FolderConfigs;
            var currentList = folderConfigList.Where(x => x.FolderPath != FolderConfig.FolderPath).ToList();
            currentList.Add(FolderConfig);

            var options = SnykVSPackage.ServiceProvider.Options;
            options.FolderConfigs = currentList;
            SnykVSPackage.ServiceProvider.SnykOptionsManager.Save(options);
           this.CloseDialog();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.CloseDialog();
        }

        private void CloseDialog()
        {
            IsOpen = false;
            this.Close();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.ShowNewFolderButton = false;

                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    SelectedFolderPath.Text = dialog.SelectedPath;
                }
            }
        }
    }
}
