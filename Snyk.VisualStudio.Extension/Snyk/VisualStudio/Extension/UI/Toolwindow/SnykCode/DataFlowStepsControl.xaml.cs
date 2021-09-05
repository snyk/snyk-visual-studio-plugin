namespace Snyk.VisualStudio.Extension.UI.Toolwindow.SnykCode
{
    using System.Windows.Controls;
    using System.Windows.Documents;
    using Snyk.VisualStudio.Extension.Service;

    /// <summary>
    /// Interaction logic for DataFlowStepsControl.xaml.
    /// </summary>
    public partial class DataFlowStepsControl : UserControl
    {
        private DataFlowStepsViewModel model;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataFlowStepsControl"/> class.
        /// </summary>
        public DataFlowStepsControl()
        {
            this.InitializeComponent();

            this.model = new DataFlowStepsViewModel();

            this.DataContext = this.model;
        }

        /// <summary>
        /// Clear steps model items and header text.
        /// </summary>
        public void Clear()
        {
            this.model.DataFlowSteps.Clear();

            this.stepsCountHeader.Text = $"Data Flow - 0 steps";
        }

        /// <summary>
        /// Add step to model and update header text.
        /// </summary>
        /// <param name="dataFlowStep">Step object to add to model.</param>
        public void AddStep(DataFlowStep dataFlowStep)
        {
            this.model.DataFlowSteps.Add(dataFlowStep);

            int stepsCount = this.model.DataFlowSteps.Count;

            this.stepsCountHeader.Text = $"Data Flow - {stepsCount} step" + (stepsCount > 1 ? "s" : string.Empty);
        }

        private void Hyperlink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var hyperlink = sender as Hyperlink;
            var navigationInformation = hyperlink.NavigateUri.ToString().Split('|');

            string filePath = navigationInformation[0].Substring(8, navigationInformation[0].Length - 8);

            string startLine = navigationInformation[1];
            string endLine = navigationInformation[2];
            string startColumn = navigationInformation[3];
            string endColumn = navigationInformation[4];

            VsCodeService.Instance.OpenAndNavigate(
                filePath, 
                int.Parse(startLine),
                int.Parse(startColumn) - 1,
                int.Parse(endLine),
                int.Parse(endColumn));
        }
    }
}
