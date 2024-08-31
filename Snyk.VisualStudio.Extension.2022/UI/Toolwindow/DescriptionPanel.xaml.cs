using Snyk.VisualStudio.Extension.Language;
using System.Windows;
using System.Windows.Controls;
using Task = System.Threading.Tasks.Task;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Interaction logic for DescriptionPanel.xaml.
    /// </summary>
    public partial class DescriptionPanel : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptionPanel"/> class. For OSS scan result.
        /// </summary>
        public DescriptionPanel()
        {
            this.InitializeComponent();
        }

        public void SetOssIssue(Issue value)
        {
            this.snykCodeDescriptionControl.Visibility = Visibility.Collapsed;
            this.ossDescriptionControl.Visibility = Visibility.Visible;

            this.descriptionHeaderPanel.OssIssue = value;
            this.ossDescriptionControl.OssIssue = value;
        }

        public async Task SetCodeIssueAsync(Issue value)
        {
            this.ossDescriptionControl.Visibility = Visibility.Collapsed;
            this.snykCodeDescriptionControl.Visibility = Visibility.Visible;
            this.descriptionHeaderPanel.CodeIssue = value;
            await this.snykCodeDescriptionControl.SetIssueAsync(value);
        }
    }
}
