using System.Linq;
using System.Windows;
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
                await this.serviceProvider.SolutionService.GetSolutionFolderAsync());
            FolderConfig =
                this.serviceProvider.Options?.FolderConfigs?.SingleOrDefault(x => x.FolderPath == currentFolder);
            if (FolderConfig == null)
                return;
            LblFolderPath.Text = FolderConfig.FolderPath;
            CbBranchList.ItemsSource = FolderConfig.LocalBranches;
            CbBranchList.SelectedItem = FolderConfig.BaseBranch;
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
            var folderConfigList = this.serviceProvider.Options.FolderConfigs;
            var currentList = folderConfigList.Where(x => x.FolderPath != FolderConfig.FolderPath).ToList();
            currentList.Add(FolderConfig);

            SnykVSPackage.ServiceProvider.Options.FolderConfigs = currentList;
     
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
    }
}
