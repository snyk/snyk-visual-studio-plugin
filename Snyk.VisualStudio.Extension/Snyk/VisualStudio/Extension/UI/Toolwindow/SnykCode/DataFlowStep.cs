namespace Snyk.VisualStudio.Extension.UI.Toolwindow.SnykCode
{
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
        /// Gets or sets navigation information. Format is 'filePath|startRow|endRow|startColumn|endColumn'.
        /// </summary>
        public string NabigationInformation { get; set; }
    }
}
