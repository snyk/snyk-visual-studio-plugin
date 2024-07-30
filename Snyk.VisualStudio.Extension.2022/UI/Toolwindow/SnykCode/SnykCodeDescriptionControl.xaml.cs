namespace Snyk.VisualStudio.Extension.UI.Toolwindow.SnykCode
{
    using System.Threading.Tasks;
    using System.Windows.Controls;
    using Snyk.Code.Library.Domain.Analysis;

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

        public async Task SetSuggestionAsync(Suggestion suggestion)
        {
            this.snykCodeDescription.Text = suggestion.Message;
            await this.dataFlowStepsControl.DisplayAsync(suggestion.Markers);
            this.externalExampleFixesControl.Display(suggestion.RepoDatasetSize, suggestion.Fixes);
        }
    }
}
