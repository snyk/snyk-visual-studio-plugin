using Snyk.VisualStudio.Extension.Language;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow.SnykCode
{
    
    /// <summary>
    /// Interaction logic for SnykCodeDescriptionControl.xaml.
    /// </summary>
    public partial class SnykCodeDescriptionControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCodeDescriptionControl"/> class.
        /// </summary>
        public SnykCodeDescriptionControl()
        {
            this.InitializeComponent();
        }

        public async Task SetIssueAsync(Issue issue)
        {
            this.snykCodeDescription.Text = issue.AdditionalData?.Message ?? "";
            await this.dataFlowStepsControl.DisplayAsync(issue.AdditionalData?.Markers);
            this.externalExampleFixesControl.Display(issue.AdditionalData?.RepoDatasetSize ?? 0, issue.AdditionalData?.ExampleCommitFixes);
        }
    }
}
