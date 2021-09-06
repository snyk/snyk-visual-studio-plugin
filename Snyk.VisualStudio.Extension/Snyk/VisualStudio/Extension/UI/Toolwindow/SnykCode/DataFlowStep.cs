namespace Snyk.VisualStudio.Extension.UI.Toolwindow.SnykCode
{
    using System.Windows.Input;

    /// <summary>
    /// Data flow step object for UI representation of SnykCode markers.
    /// </summary>
    public class DataFlowStep
    {
        /// <summary>
        /// Gets or sets row number in source file.
        /// </summary>
        public string RowNumber { get; set; }

        /// <summary>
        /// Gets or sets file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets line content.
        /// </summary>
        public string LineContent { get; set; }

        /// <summary>
        /// Gets or sets navigation command.
        /// </summary>
        public ICommand NavigateCommand { get; set; }
    }
}
