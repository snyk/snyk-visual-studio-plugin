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

            return new TestSearchTask(dwCookie, pSearchQuery, pSearchCallback, this);
        }

        public override void ClearSearch()
        {
            SnykToolWindowControl toolWindowControl = (SnykToolWindowControl) this.Content;

            toolWindowControl.VulnerabilitiesTree.DisplayAllVulnerabilities();
        }

        internal class TestSearchTask : VsSearchTask
        {
            private SnykToolWindow m_toolWindow;

            public TestSearchTask(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback, SnykToolWindow toolwindow)
                : base(dwCookie, pSearchQuery, pSearchCallback)
            {
                m_toolWindow = toolwindow;
            }

            protected override void OnStartSearch()
            {                
                SnykToolWindowControl toolWindowControl = (SnykToolWindowControl)m_toolWindow.Content;
                
                this.ErrorCode = VSConstants.S_OK;

                try
                {
                    string searchString = this.SearchQuery.SearchString;

                    toolWindowControl.VulnerabilitiesTree.FilterBy(searchString);                    
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
}
