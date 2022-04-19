namespace Snyk.VisualStudio.Extension.Shared.UI.Toolwindow.SnykCode
{
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

        /// <summary>
        /// Sets <see cref="Suggestion"/> information and update corresponding UI elements. For SnykCode scan result.
        /// </summary>
        public Suggestion Suggestion
        {
            set
            {
                var suggestion = value;

                this.snykCodeDescription.Text = suggestion.Message;

                this.dataFlowStepsControl.DisplayAsync(suggestion.Markers);

                this.externalExampleFixesControl.Display(suggestion.RepoDatasetSize, suggestion.Fixes);
            }
        }
    }
}
