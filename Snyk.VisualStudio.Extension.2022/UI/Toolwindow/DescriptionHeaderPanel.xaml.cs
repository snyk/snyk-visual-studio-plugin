﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Snyk.Code.Library.Domain.Analysis;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Model;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    /// <summary>
    /// Interaction logic for DescriptionHeaderPanel.xaml.
    /// </summary>
    public partial class DescriptionHeaderPanel : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptionHeaderPanel"/> class.
        /// </summary>
        public DescriptionHeaderPanel() => this.InitializeComponent();

        public Issue OssIssue
        {
            set
            {
                var issue = value;

                this.severityImage.Source = SnykIconProvider.GetSeverityIconSource(issue.Severity);

                this.issueTitle.Text = issue.Title;

                if (!string.IsNullOrEmpty(issue.AdditionalData?.License))
                {
                    this.metaType.Text = "License"; // todo: Vulnerability or License or Issue.
                }
                else
                {
                    this.metaType.Text = "Vulnerability";
                }

                var identifiers = issue.AdditionalData.Identifiers;

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

                if (!string.IsNullOrEmpty(issue.AdditionalData.CVSSv3) && (float.TryParse(issue.AdditionalData.CvssScore, out var cvssScore) && cvssScore > -1))
                {
                    this.cvssLinkBlock.Visibility = Visibility.Visible;

                    this.cvssLink.NavigateUri = new Uri("https://www.first.org/cvss/calculator/3.1#" + issue.AdditionalData.CvssScore);
                    this.cvssLinkText.Text = "CVSS " + issue.AdditionalData.CvssScore;
                }
                else
                {
                    this.cvssLinkBlock.Visibility = Visibility.Collapsed;
                }

                this.vulnerabilityIdLinkBlock.Visibility = Visibility.Visible;

                this.cvssLink.NavigateUri = new Uri(issue.GetVulnerabilityUrl());
                this.cvssLinkText.Text = issue.AdditionalData.RuleId;
            }
        }

        public Issue CodeIssue
        {
            set
            {
                var issue = value;

                this.cvePanel.Visibility = Visibility.Collapsed;
                this.cvssLinkBlock.Visibility = Visibility.Collapsed;
                this.vulnerabilityIdLinkBlock.Visibility = Visibility.Collapsed;

                this.severityImage.Source = SnykIconProvider.GetSeverityIconSource(issue.Severity);

                this.issueTitle.Text = issue.GetDisplayTitle();

                this.metaType.Text = issue.AdditionalData?.IsSecurityType ?? false ? "Vulnerability" : "Code Issue";

                if (issue.AdditionalData?.Cwe != null && issue.AdditionalData.Cwe.Count > 0)
                {
                    this.AddLinksToPanel(
                            this.cwePanel,
                            issue.AdditionalData.Cwe.ToArray(),
                            "CWE-",
                            "https://cwe.mitre.org/data/definitions/{0}.html");
                }

                this.cwePanel.Visibility = this.cwePanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OnLinkClick(object obj, RoutedEventArgs eventArgs)
        {
            if (obj is Hyperlink link)
            {
                Process.Start(new ProcessStartInfo(link.NavigateUri.AbsoluteUri));
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
                link.Click += OnLinkClick;

                textBlock.Inlines.Add(link);

                panel.Children.Add(textBlock);
            }
        }
    }
}