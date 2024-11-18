using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Snyk.VisualStudio.Extension.Model;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
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
        public SnykToolWindow()
            : base(null)
        {
            this.Caption = "Snyk";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new SnykToolWindowControl(this);

            this.ToolBar = new CommandID(SnykGuids.SnykVSPackageCommandSet, SnykGuids.SnykToolbarId);

            this.ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;
        }

        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var toolWindowControl = Content as SnykToolWindowControl;
                if (toolWindowControl == null) return;
                var package = Package as SnykVSPackage;
                if (package == null) return;

                package.ToolWindow = this;
                package.ToolWindowControl = toolWindowControl;
                var serviceProvider = await package.GetServiceAsync(typeof(SnykService)) as SnykService ??
                    throw new InvalidOperationException("Could not find Snyk Service");
                toolWindowControl.InitializeEventListeners(serviceProvider);
                toolWindowControl.Initialize(serviceProvider);
            });
          
        }

        /// <summary>
        /// Gets a value indicating whether True. Enable search in tool window.
        /// </summary>
        public override bool SearchEnabled => true;

        /// <summary>
        /// Gets a value indicating whether <see cref="IVsEnumWindowSearchFilters"/>.
        /// </summary>
        public override IVsEnumWindowSearchFilters SearchFiltersEnum
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                List<IVsWindowSearchFilter> severityFilters = new List<IVsWindowSearchFilter>();

                severityFilters.Add(new WindowSearchSimpleFilter(SeverityFilter.CriticalTitle, SeverityFilter.CriticalTitle, "severity", Severity.Critical));
                severityFilters.Add(new WindowSearchSimpleFilter(SeverityFilter.HighTitle, SeverityFilter.HighTitle, "severity", Severity.High));
                severityFilters.Add(new WindowSearchSimpleFilter(SeverityFilter.MediumTitle, SeverityFilter.MediumTitle, "severity", Severity.Medium));
                severityFilters.Add(new WindowSearchSimpleFilter(SeverityFilter.LowTitle, SeverityFilter.LowTitle, "severity", Severity.Low));

                return new WindowSearchFilterEnumerator(severityFilters) as IVsEnumWindowSearchFilters;
            }
        }

        /// <summary>
        /// Create <see cref="VulnerabilitySearchTask"/>.
        /// </summary>
        /// <param name="dwCookie">Cookie.</param>
        /// <param name="pSearchQuery">Search query.</param>
        /// <param name="pSearchCallback">Search callback.</param>
        /// <returns>IVsSearchTask implementation.</returns>
        public override IVsSearchTask CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
        {
            if (pSearchQuery == null || pSearchCallback == null)
            {
                return null;
            }

            return new VulnerabilitySearchTask(dwCookie, pSearchQuery, pSearchCallback, this);
        }

        /// <summary>
        /// Clear search bar content.
        /// </summary>
        public override void ClearSearch()
        {
            var toolWindowControl = (SnykToolWindowControl) this.Content;

            toolWindowControl.VulnerabilitiesTree.DisplayAllVulnerabilities();
        }

        /// <summary>
        /// Provide search settings.
        /// </summary>
        /// <param name="pSearchSettings">Search settings.</param>
        public override void ProvideSearchSettings(IVsUIDataSource pSearchSettings)
            => Utilities.SetValue(pSearchSettings, SearchSettingsDataSource.SearchStartTypeProperty.Name, (uint)VSSEARCHSTARTTYPE.SST_INSTANT);

        /// <summary>
        /// Implementation of <see cref="VsSearchTask"/>.
        /// </summary>
        internal class VulnerabilitySearchTask : VsSearchTask
        {
            private readonly SnykToolWindow toolWindow;

            /// <summary>
            /// Initializes a new instance of the <see cref="VulnerabilitySearchTask"/> class.
            /// </summary>
            /// <param name="dwCookie">Cookie.</param>
            /// <param name="pSearchQuery">Search query</param>
            /// <param name="pSearchCallback">Search callback.</param>
            /// <param name="toolwindow">Tool window.</param>
            public VulnerabilitySearchTask(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback, SnykToolWindow toolwindow)
                : base(dwCookie, pSearchQuery, pSearchCallback) => this.toolWindow = toolwindow;

            /// <summary>
            /// Get search string logic and delegate logic to <see cref="SnykFilterableTree"/>.
            /// </summary>
            protected override void OnStartSearch()
            {
                this.ErrorCode = VSConstants.S_OK;

                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    try
                    {
                        var toolWindowControl = (SnykToolWindowControl)this.toolWindow.Content;

                        toolWindowControl.VulnerabilitiesTree.FilterBy(this.SearchQuery.SearchString);
                    }
                    catch (Exception)
                    {
                        this.ErrorCode = VSConstants.E_FAIL;
                    }
                });

                base.OnStartSearch();
            }

            /// <summary>
            /// Stop search event handler. Set search results to 0.
            /// </summary>
            protected override void OnStopSearch() => this.SearchResults = 0;
        }
    }
}
