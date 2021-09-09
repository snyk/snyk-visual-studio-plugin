namespace Snyk.VisualStudio.Extension.UI.Toolwindow.SnykCode
{
    using System.Windows.Controls;

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
    }
}
