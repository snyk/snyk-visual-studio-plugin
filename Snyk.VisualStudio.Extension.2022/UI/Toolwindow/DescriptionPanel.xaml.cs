using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    using System.Windows;
    using System.Windows.Controls;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.VisualStudio.Extension.CLI;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Interaction logic for DescriptionPanel.xaml.
    /// </summary>
    public partial class DescriptionPanel : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptionPanel"/> class. For OSS scan result.
        /// </summary>
        public DescriptionPanel()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Sets <see cref="Vulnerability"/> information and update corresponding UI elements.
        /// </summary>
        public Vulnerability Vulnerability
        {
            set
            {
                this.snykCodeDescriptionControl.Visibility = Visibility.Collapsed;
                this.ossDescriptionControl.Visibility = Visibility.Visible;

                this.descriptionHeaderPanel.Vulnerability = value;
                this.ossDescriptionControl.Vulnerability = value;
            }
        }

        public async Task SetIssueAsync(Issue value)
        {
            this.ossDescriptionControl.Visibility = Visibility.Collapsed;
            this.snykCodeDescriptionControl.Visibility = Visibility.Visible;
            this.descriptionHeaderPanel.Issue = value;
            await this.snykCodeDescriptionControl.SetIssueAsync(value);
        }
    }
}
