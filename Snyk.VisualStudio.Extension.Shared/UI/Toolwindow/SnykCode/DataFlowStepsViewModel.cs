namespace Snyk.VisualStudio.Extension.Shared.UI.Toolwindow.SnykCode
{
    using System.Collections.ObjectModel;

    /// <summary>
    /// Data Flow Steps View Model.
    /// </summary>
    public class DataFlowStepsViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataFlowStepsViewModel"/> class.
        /// </summary>
        public DataFlowStepsViewModel() => this.DataFlowSteps = new ObservableCollection<DataFlowStep>();

        /// <summary>
        /// Gets or sets collection of <see cref="DataFlowStep"/> objects.
        /// </summary>
        public ObservableCollection<DataFlowStep> DataFlowSteps { get; set; }
    }
}
