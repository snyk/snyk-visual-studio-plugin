namespace Snyk.VisualStudio.Extension.UI.Toolwindow.SnykCode
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Windows.Controls;
    using Microsoft.VisualStudio.PlatformUI;
    using Serilog;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.CLI;
    using Snyk.VisualStudio.Extension.Service;

    /// <summary>
    /// Interaction logic for SnykCodeDescriptionControl.xaml.
    /// </summary>
    public partial class SnykCodeDescriptionControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykCodeDescriptionControl>();

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

                var index = 1;

                this.dataFlowStepsControl.Clear();

                var markers = suggestion.Markers.Distinct(new MarkerByPositionLineComparer());

                foreach (var marker in markers)
                {
                    foreach (var position in marker.Positions)
                    {
                        string filePosition = position.FileName;
                        int fileSeparatorIndex = filePosition.LastIndexOf("/") + 1;
                        filePosition = filePosition.Substring(fileSeparatorIndex, filePosition.Length - fileSeparatorIndex);

                        long startLineNumber = position.Rows.ElementAt(0);

                        filePosition = filePosition + ":" + startLineNumber;

                        var startLine = (int)position.Rows.ElementAt(0) - 1;
                        var endLine = (int)position.Rows.ElementAt(1) - 1;
                        var startColumn = (int)position.Columns.ElementAt(0) - 1;
                        var endColumn = (int)position.Columns.ElementAt(1);

                        var dataFlowStep = new DataFlowStep
                        {
                            FileName = filePosition,
                            RowNumber = index.ToString(),
                            LineContent = this.GetLineContent(position.FileName, startLineNumber),
                            NavigateCommand = new DelegateCommand(new Action<object>(delegate (object o)
                            {
                                VsCodeService.Instance.OpenAndNavigate(this.GetFullPath(position.FileName), startLine, startColumn, endLine, endColumn);
                            })),
                        };

                        index++;

                        this.dataFlowStepsControl.AddStep(dataFlowStep);
                    }
                }

                this.externalExampleFixesControl.DisplayFixes(suggestion.Fixes);
            }
        }

        private string GetLineContent(string file, long lineNumber)
        {
            string filePath = this.GetFullPath(file);

            string line = string.Empty;

            try
            {
                int fileLineNumber = 0;

                using (var reader = new StreamReader(filePath))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (fileLineNumber == (lineNumber - 1))
                        {
                            return line.Trim();
                        }

                        fileLineNumber++;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }

            return line;
        }

        private string GetFullPath(string file)
        {
            string partialPath = file.Substring(1, file.Length - 1);

            string solutionPath = SnykSolutionService.Instance.GetSolutionPath();

            return Path.Combine(solutionPath, partialPath);
        }
    }
}
