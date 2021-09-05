using Snyk.VisualStudio.Extension.Service;

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

        public string NavigateCommand
        {
            get
            {
                VsCodeService.Instance.OpenAndNavigate(
                    this.FilePath,
                    this.StartLine,
                    this.StartColumn - 1,
                    this.EndLine,
                    this.EndColumn);

                return string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets full file path.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets start line number.
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// Gets or sets end line number.
        /// </summary>
        public int EndLine { get; set; }

        /// <summary>
        /// Gets or sets start column number.
        /// </summary>
        public int StartColumn { get; set; }

        /// <summary>
        /// Gets or sets end column number.
        /// </summary>
        public int EndColumn { get; set; }
    }
}
