//------------------------------------------------------------------------------
// <copyright file="SnykVSPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Snyk.VisualStudio.Extension.UI;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.Services;
using Microsoft.VisualStudio;

namespace Snyk.VisualStudio.Extension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(SnykVSPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(SnykToolWindow))]
    [ProvideOptionPage(typeof(SnykGeneralOptionsDialogPage), "Snyk", "General settings", 1000, 1001, true)]
    [ProvideOptionPage(typeof(SnykProjectOptionsDialogPage), "Snyk", "Project settings", 1000, 1002, true)]
    public sealed class SnykVSPackage : Package
    {
        /// <summary>
        /// SnykVSPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "5ddf9abb-42ec-49b9-b201-b3e2fc2f8f89";

        private static SnykVSPackage instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykVSPackage"/> class.
        /// </summary>
        public SnykVSPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
            instance = this;
        }             

        public SnykSolutionService SolutionService
        {
            get
            {
                return SnykSolutionService.Instance;
            }
        }

        public ISnykOptions Options
        {
            get
            {
                return SnykGeneralOptionsDialogPage;
            }
        }               

        public void ShowToolWindow()
        {
            ToolWindowPane toolWindowPane = FindToolWindow(typeof(SnykToolWindow), 0, true);

            if ((null == toolWindowPane) || (null == toolWindowPane.Frame))
            {
                throw new NotSupportedException("Cannot create window.");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)toolWindowPane.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public SnykToolWindowControl GetToolWindow()
        {
            ToolWindowPane toolWindowPane = FindToolWindow(typeof(SnykToolWindow), 0, true);

            if ((null == toolWindowPane) || (null == toolWindowPane.Frame))
            {
                throw new NotSupportedException("Cannot find Snyk tool window.");
            }

            return (SnykToolWindowControl)toolWindowPane.Content;
        }

        private SnykGeneralOptionsDialogPage SnykGeneralOptionsDialogPage
        {
            get
            {
                return (SnykGeneralOptionsDialogPage)GetDialogPage(typeof(SnykGeneralOptionsDialogPage));
            }            
        }               

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            SnykTasksService.Initialize(this);
            SnykSolutionService.Initialize(this);
            SnykRunScanCommand.Initialize(this);
            SnykToolWindowCommand.Initialize(this);
            SnykGeneralOptionsDialogPage.Initialize(this);

            GetToolWindow().Package = this;

            InitializeEventListeners();

            SnykTasksService.Instance().Download();
        }

        private void InitializeEventListeners()
        {
            var toolWindow = GetToolWindow();
            var tasksService = SnykTasksService.Instance();

            tasksService.ScanError += toolWindow.OnDisplayError;
            tasksService.ScanningCancelled += toolWindow.OnScanningCancelled;
            tasksService.ScanningStarted += toolWindow.OnScanningStarted;
            tasksService.ScanningUpdate += toolWindow.OnScanningUpdate;
            tasksService.ScanningFinished += toolWindow.OnOnScanningFinished;

            tasksService.DownloadStarted += toolWindow.OnDownloadStarted;
            tasksService.DownloadFinished += toolWindow.OnDownloadFinished;
            tasksService.DownloadUpdate += toolWindow.OnDownloadUpdate;
            tasksService.DownloadCancelled += toolWindow.OnDownloadCancelled;
        }

        #endregion
    }
}
