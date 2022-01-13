namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.VisualStudio.Extension.CLI;

    /// <summary>
    /// Interaction logic for DescriptionHeaderPanel.xaml.
    /// </summary>
    public partial class DescriptionHeaderPanel : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptionHeaderPanel"/> class.
        /// </summary>
        public DescriptionHeaderPanel() => this.InitializeComponent();

        public Vulnerability Vulnerability
        {
            set
            {
                var vulnerability = value;

                this.severityImage.Source = SnykIconProvider.GetSeverityIconSource(vulnerability.Severity);

                this.issueTitle.Text = vulnerability.Title;

                if (vulnerability.IsLicense())
                {
                    this.metaType.Text = "License"; // todo: Vulnerability or License or Issue.
                }
                else
                {
                    this.metaType.Text = "Vulnerability";
                }

                var identifiers = vulnerability.Identifiers;

                if (identifiers != null)
                {
                    this.AddLinksToPanel(
                            this.cwePanel,
                            identifiers.CWE,
                            "CWE-",
                            "https://cwe.mitre.org/data/definitions/{0}.html");

                    this.AddLinksToPanel(
                            this.cvePanel,
                            identifiers.CVE,
                            "CVE-",
                            "https://cve.mitre.org/cgi-bin/cvename.cgi?name={0}");
                }
                else
                {
                    this.cwePanel.Visibility = Visibility.Collapsed;
                    this.cvePanel.Visibility = Visibility.Collapsed;
                }

                this.cwePanel.Visibility = this.cwePanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                this.cvePanel.Visibility = this.cvePanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                if (!string.IsNullOrEmpty(vulnerability.CVSSv3) && vulnerability.CvssScore > -1)
                {
                    this.cvssLinkBlock.Visibility = Visibility.Visible;

                    this.cvssLink.NavigateUri = new Uri("https://www.first.org/cvss/calculator/3.1#" + vulnerability.CVSSv3);
                    this.cvssLinkText.Text = "CVSS " + vulnerability.CvssScore;
                }
                else
                {
                    this.cvssLinkBlock.Visibility = Visibility.Collapsed;
                }

                this.vulnerabilityIdLinkBlock.Visibility = Visibility.Visible;

                this.cvssLink.NavigateUri = new Uri(vulnerability.Url);
                this.cvssLinkText.Text = vulnerability.Id;
                this.cvssLink.Click += new RoutedEventHandler(delegate (object obj, RoutedEventArgs eventArgs)
                {
                    if (obj is Hyperlink)
                    {
                        var link = obj as Hyperlink;

                        Process.Start(new ProcessStartInfo(link.NavigateUri.AbsoluteUri));
                    }
                });
            }
        }

        public Suggestion Suggestion
        {
            set
            {
                var suggestion = value;

                this.cvePanel.Visibility = Visibility.Collapsed;
                this.cvssLinkBlock.Visibility = Visibility.Collapsed;
                this.vulnerabilityIdLinkBlock.Visibility = Visibility.Collapsed;

                this.severityImage.Source = SnykIconProvider.GetSeverityIconSource(Severity.FromInt(suggestion.Severity));

                this.issueTitle.Text = suggestion.GetDisplayTitle();

                if (suggestion.Categories.Contains("Security"))
                {
                    this.metaType.Text = "Vulnerability";
                }
                else
                {
                    this.metaType.Text = "Code Issue";
                }

                if (suggestion.Cwe != null && suggestion.Cwe.Count > 0)
                {
                    this.AddLinksToPanel(
                            this.cwePanel,
                            suggestion.Cwe.ToArray(),
                            "CWE-",
                            "https://cwe.mitre.org/data/definitions/{0}.html");
                }

                this.cwePanel.Visibility = this.cwePanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void AddLinksToPanel(StackPanel panel, string[] linkIds, string namePrefix, string urlPatter)
        {
            panel.Children.Clear();

            if (linkIds == null || linkIds.Length == 0)
            {
                return;
            }

            foreach (var linkData in linkIds)
                {
                var textBlock = new TextBlock();
                var link = new Hyperlink();

                    link.NavigateUri = new Uri(string.Format(urlPatter, linkData.Replace(namePrefix, string.Empty)));
                    link.Inlines.Add(linkData);
                link.Click += new RoutedEventHandler(delegate (object obj, RoutedEventArgs args)
                    {
                        if (obj is Hyperlink)
                        {
                            var hyperlink = obj as Hyperlink;

                            Process.Start(new ProcessStartInfo(hyperlink.NavigateUri.AbsoluteUri));
                        }
                    });

                    textBlock.Inlines.Add(link);

                    panel.Children.Add(textBlock);
                }
            }
    }
}