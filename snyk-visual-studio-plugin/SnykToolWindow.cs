//------------------------------------------------------------------------------
// <copyright file="SnykToolWindow.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Snyk.VisualStudio.Extension.UI
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.Internal.VisualStudio.PlatformUI;
    using Microsoft.VisualStudio.PlatformUI;
    using System.Collections.Generic;
    using CLI;
    using System.Text;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("b38c6cbc-524d-4f30-8a18-936e3104b734")]
    public class SnykToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnykToolWindow"/> class.
        /// </summary>
        public SnykToolWindow() : base(null)
        {
            this.Caption = "Snyk";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new SnykToolWindowControl();
        }

        public override bool SearchEnabled
        {
            get { return true; }
        }

        public override IVsSearchTask CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
        {
            if (pSearchQuery == null || pSearchCallback == null)
            {
                return null;
            }

            return new SnykSearchTask(dwCookie, pSearchQuery, pSearchCallback, this);
        }

        public override void ClearSearch()
        {
            SnykToolWindowControl toolWindowControl = (SnykToolWindowControl) this.Content;

            toolWindowControl.VulnerabilitiesTree.DisplayAllVulnerabilities();
        }

        public override void ProvideSearchSettings(IVsUIDataSource pSearchSettings)
        {
            Utilities.SetValue(pSearchSettings, SearchSettingsDataSource.SearchStartTypeProperty.Name, (uint)VSSEARCHSTARTTYPE.SST_INSTANT);
        }

        public override IVsEnumWindowSearchFilters SearchFiltersEnum
        {
            get
            {
                List<IVsWindowSearchFilter> list = new List<IVsWindowSearchFilter>();

                list.Add(new WindowSearchSimpleFilter("High severity", "High severity", "severity", Severity.High));
                list.Add(new WindowSearchSimpleFilter("Medium severity", "Medium severity", "severity", Severity.Medium));
                list.Add(new WindowSearchSimpleFilter("Low severity", "Low severity", "severity", Severity.Low));

                return new WindowSearchFilterEnumerator(list) as IVsEnumWindowSearchFilters;
            }
        }

        internal class SnykSearchTask : VsSearchTask
        {
            private const string SeverityHighFilter = "severity:\"high\"";
            private const string SeverityMediumFilter = "severity:\"medium\"";
            private const string SeverityLowFilter = "severity:\"low\"";

            private readonly SnykToolWindow toolWindow;

            public SnykSearchTask(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback, SnykToolWindow toolwindow)
                : base(dwCookie, pSearchQuery, pSearchCallback)
            {
                toolWindow = toolwindow;
            }

            protected override void OnStartSearch()
            {                
                SnykToolWindowControl toolWindowControl = (SnykToolWindowControl)toolWindow.Content;
                
                this.ErrorCode = VSConstants.S_OK;

                try
                {
                    string searchString = this.SearchQuery.SearchString;

                    SeverityCaseOptions severityOptions = null;

                    if (searchString.Contains(SeverityHighFilter) 
                        || searchString.Contains(SeverityMediumFilter) 
                        || searchString.Contains(SeverityLowFilter)) 
                    {
                        severityOptions = new SeverityCaseOptions
                        {
                            IsHighIncluded = searchString.Contains(SeverityHighFilter),
                            IsMediumIncluded = searchString.Contains(SeverityMediumFilter),
                            IsLowIncluded = searchString.Contains(SeverityLowFilter)
                        };

                        StringBuilder stringBuilder = new StringBuilder(searchString);
                        stringBuilder.Replace(SeverityHighFilter, "");
                        stringBuilder.Replace(SeverityMediumFilter, "");
                        stringBuilder.Replace(SeverityLowFilter, "");

                        searchString = stringBuilder.ToString().Trim();
                    }

                    toolWindowControl.VulnerabilitiesTree.FilterBy(searchString, severityOptions);
                }
                catch (Exception exception)
                {
                    this.ErrorCode = VSConstants.E_FAIL;
                }

                base.OnStartSearch();
            }

            protected override void OnStopSearch()
            {
                this.SearchResults = 0;
            }
        }        
    }

    public class SeverityCaseOptions
    {
        public bool IsHighIncluded { get; set; }
        public bool IsMediumIncluded { get; set; }
        public bool IsLowIncluded { get; set; }
    }
}
