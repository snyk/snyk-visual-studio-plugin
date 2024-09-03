using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.Common;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Task = System.Threading.Tasks.Task;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow.SnykCode
{

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
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [SuppressMessage(
            "Usage",
            "VSTHRD012:Provide JoinableTaskFactory where allowed",
            Justification = "Only possible in VS22, and DelegateCommand might be replaced with a different ICommand implementation")]
        internal async Task DisplayAsync(IList<Marker> markers)
        {
            this.Clear();
            
            this.Visibility = markers != null && markers.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (markers == null)
            {
                return;
            }
            var index = 1;

            var dataFlowSteps = new HashSet<DataFlowStep>();

            foreach (var marker in markers)
            {
                foreach (var position in marker.Pos)
                {
                    var filePosition = position.File;
                    var fileSeparatorIndex = filePosition.LastIndexOf("/") + 1;
                    filePosition = filePosition.Substring(fileSeparatorIndex, filePosition.Length - fileSeparatorIndex);

                    long startLineNumber = position.Rows.ElementAt(0);

                    filePosition = filePosition + ":" + startLineNumber;

                    var startLine = (int)position.Rows.ElementAt(0);
                    var endLine = (int)position.Rows.ElementAt(1);
                    var startColumn = (int)position.Cols.ElementAt(0);
                    var endColumn = (int)position.Cols.ElementAt(1);

                    var dataFlowStep = new DataFlowStep
                    {
                        FileName = filePosition,
                        RowNumber = index.ToString(),
                        LineContent = await this.GetLineContentAsync(position.File, startLineNumber),
                        NavigateCommand = new DelegateCommand((obj) =>
                            this.NavigateToCodeAsync(position.File, startLine, startColumn, endLine, endColumn).FireAndForget()),
                    };

                    index++;

                    dataFlowSteps.Add(dataFlowStep);
                }
            }

            this.AddDataFlowSteps(dataFlowSteps.ToList());
        }

        private async Task NavigateToCodeAsync(string fileName, int startLine, int startColumn, int endLine, int endColumn)
        {
            try
            {
                VsCodeService.Instance.OpenAndNavigate(
                    fileName,
                    startLine,
                    startColumn,
                    endLine,
                    endColumn);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error on open and navigate to source code");
            }
        }

        private async Task<string> GetLineContentAsync(string file, long lineNumber)
        {
            var line = string.Empty;

            try
            {
                var fileLineNumber = 0;

                using (var reader = new StreamReader(file))
                {
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (fileLineNumber == lineNumber)
                        {
                            return line.Trim();
                        }

                        fileLineNumber++;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error on get editor line content");
            }

            return line;
        }
    }
}