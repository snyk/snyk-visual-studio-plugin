namespace Snyk.VisualStudio.Extension.UI.Toolwindow.SnykCode
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.VisualStudio.PlatformUI;
    using Serilog;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.CLI;
    using Snyk.VisualStudio.Extension.Service;

    /// <summary>
    /// Interaction logic for DataFlowStepsControl.xaml.
    /// </summary>
    public partial class DataFlowStepsControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.ForContext<DataFlowStepsControl>();

        private DataFlowStepsViewModel model;

        private SnykSolutionService solutionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataFlowStepsControl"/> class.
        /// </summary>
        public DataFlowStepsControl()
        {
            this.InitializeComponent();

            this.model = new DataFlowStepsViewModel();

            this.DataContext = this.model;

            this.solutionService = SnykSolutionService.Instance;
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
        /// <param name="dataFlowSteps">Step object to add to model.</param>
        public void AddDataFlowSteps(IList<DataFlowStep> dataFlowSteps)
        {
            foreach (var dataFlowStep in dataFlowSteps)
            {
                this.model.DataFlowSteps.Add(dataFlowStep);
            }

            int stepsCount = this.model.DataFlowSteps.Count;

            this.stepsCountHeader.Text = $"Data Flow - {stepsCount} step" + (stepsCount > 1 ? "s" : string.Empty);
        }

        /// <summary>
        /// Add markers to panel.
        /// </summary>
        /// <param name="markers">Markers from suggestion.</param>
        internal void Display(IList<Marker> markers)
        {
            this.Clear();

            this.Visibility = markers != null && markers.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            var index = 1;

            var dataFlowSteps = new List<DataFlowStep>();

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
                            VsCodeService.Instance.OpenAndNavigate(
                                this.solutionService.GetFileFullPath(position.FileName),
                                startLine,
                                startColumn,
                                endLine,
                                endColumn);
                        })),
                    };

                    index++;

                    dataFlowSteps.Add(dataFlowStep);
                }
            }

            this.AddDataFlowSteps(dataFlowSteps.Distinct().ToList());
        }

        private string GetLineContent(string file, long lineNumber)
        {
            string filePath = this.solutionService.GetFileFullPath(file);

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
    }
}