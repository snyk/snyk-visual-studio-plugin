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

        /// <summary>
        /// Compare by file name and line content.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>True if file name and line content are equal.</returns>
        public override bool Equals(object obj) => this.Equals(obj as DataFlowStep);

        /// <summary>
        /// Get hash code using fileName and lineContent.
        /// </summary>
        /// <returns>Int hash value.</returns>
        public override int GetHashCode() => (this.FileName, this.LineContent).GetHashCode();

        private bool Equals(DataFlowStep dataFlowStep)
        {
            if (dataFlowStep is null)
            {
                return false;
            }

            if (object.ReferenceEquals(this, dataFlowStep))
            {
                return true;
            }

            return (this.FileName == dataFlowStep.FileName) && (this.LineContent == dataFlowStep.LineContent);
        }
    }
}
