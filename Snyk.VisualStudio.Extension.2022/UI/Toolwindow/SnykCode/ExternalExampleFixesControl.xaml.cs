using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow.SnykCode
{
    /// <summary>
    /// Interaction logic for ExternalExampleFixesControl.xaml.
    /// </summary>
    public partial class ExternalExampleFixesControl : UserControl
    {
        private const int ShowExamplesCount = 3;

        private ObservableCollection<ExampleFixTab> tabs = new ObservableCollection<ExampleFixTab>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalExampleFixesControl"/> class.
        /// </summary>
        public ExternalExampleFixesControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Display fixes in tabs.
        /// </summary>
        /// <param name="repoDatasetSize">Count of repositories with fixed this issue.</param>
        /// <param name="fixes">Suggestion fixes.</param>
        internal void Display(int repoDatasetSize, IList<ExampleCommitFix> fixes)
        {
            this.tabs.Clear();

            this.Visibility = fixes != null && fixes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (fixes == null || fixes.Count == 0)
            {
                this.shortDescription.Text = "No example fixes available.";

                return;
            }

            var examplesCount = fixes.Count > ShowExamplesCount ? ShowExamplesCount : fixes.Count;

            this.shortDescription.Text = $"This issue was fixed by {repoDatasetSize} projects. Here are {examplesCount} example fixes.";

            for (int i = 0; i < examplesCount; i++)
            {
                var fix = fixes.ElementAt(i);

                this.tabs.Add(new ExampleFixTab
                {
                    Title = this.GetGithubRepositoryName(fix.CommitURL),
                    Lines = new ObservableCollection<LineData>(fix.Lines),
                });
            }

            if (this.externalExampleFixesTab.ItemsSource == null)
            {
                this.externalExampleFixesTab.ItemsSource = this.tabs;
            }
        }

        private string GetGithubRepositoryName(string url)
        {
            var fixUrl = url.Replace("https://", string.Empty);

            fixUrl = fixUrl.IndexOf("/commit/") != -1 ? fixUrl.Substring(0, fixUrl.IndexOf("/commit/")) : fixUrl;

            return fixUrl.Length > 50 ? fixUrl.Substring(0, 50) + "..." : fixUrl;
        }
    }
}
