namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    using System.Windows;
    using System.Windows.Controls;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.VisualStudio.Extension.CLI;

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

                this.descriptionHeaderPanel.Vulnerability = value;

                this.ossDescriptionControl.Vulnerability = value;
            }
        }

        /// <summary>
        /// Sets <see cref="Suggestion"/> information and update corresponding UI elements. For SnykCode scan result.
        /// </summary>
        public Suggestion Suggestion
        {
            set
            {
                this.ossDescriptionControl.Visibility = Visibility.Collapsed;

                this.descriptionHeaderPanel.Suggestion = value;

                this.snykCodeDescriptionControl.Suggestion = value;
            }
        }
    }
}
