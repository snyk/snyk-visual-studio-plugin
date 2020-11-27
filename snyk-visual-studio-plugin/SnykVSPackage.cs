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
using System.Windows.Forms;

namespace snyk_visual_studio_plugin
{
    [Guid("6558dc66-aad3-41d6-84ed-8bea01fc852d")]
    public class SnykProjectOptionsDialogPage : DialogPage
    {        
        protected override IWin32Window Window
        {
            get
            {
                SnykProjectOptionsUserControl optionsUserControl = new SnykProjectOptionsUserControl(SnykVSPackage.GetInstance());
                optionsUserControl.projectOptionsPage = this;
                optionsUserControl.Initialize();

                return optionsUserControl;
            }
        }
    }

    [Guid("d45468c1-33d2-4dca-9780-68abaedf95e7")]
    public class SnykGeneralOptionsDialogPage : DialogPage
    {
        private string apiToken = "";
        private string customEndpoint = "";
        private string organization = "";
        private bool ignoreUnknownCA = false;

        public string ApiToken
        {
            get { return apiToken; }
            set { apiToken = value; }
        }

        public string CustomEndpoint
        {
            get { return customEndpoint; }
            set { customEndpoint = value; }
        }

        public string Organization
        {
            get { return organization; }
            set { organization = value; }
        }

        public bool IgnoreUnknownCA
        {
            get { return ignoreUnknownCA; }
            set { ignoreUnknownCA = value; }
        }

        protected override IWin32Window Window
        {
            get
            {
                SnykGeneralSettingsUserControl generalSettingsUserControl = new SnykGeneralSettingsUserControl();

                generalSettingsUserControl.optionsDialogPage = this;
                generalSettingsUserControl.Initialize();

                return generalSettingsUserControl;
            }
        }
    }

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

        public static SnykVSPackage GetInstance()
        {
            return instance;
        }

        public string ApiToken
        {
            get
            {
                return GetSnykGeneralOptionsDialogPage().ApiToken;
            }
        }

        public string Organization
        {
            get
            {
                return GetSnykGeneralOptionsDialogPage().Organization;
            }
        }

        public bool IgnoreUnknownCA
        {
            get
            {
                return GetSnykGeneralOptionsDialogPage().IgnoreUnknownCA;
            }
        }

        public string CustomEndpoint
        {
            get
            {
                return GetSnykGeneralOptionsDialogPage().CustomEndpoint;
            }
        }

        public string AdditionalOptions
        {
            get
            {
                return SnykProjectSettingsService.NewInstance(this).GetAdditionalOptions();
            }
        }

        private SnykGeneralOptionsDialogPage GetSnykGeneralOptionsDialogPage()
        {
            return (SnykGeneralOptionsDialogPage) GetDialogPage(typeof(SnykGeneralOptionsDialogPage));
        }
        
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            SnykCliDownloader cliDownloader = new SnykCliDownloader();

            cliDownloader.Download();
            SnykRunScanCommand.Initialize(this);
            SnykToolWindowCommand.Initialize(this);
        }

        #endregion
    }
}
