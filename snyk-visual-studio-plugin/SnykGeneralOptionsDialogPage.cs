using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.UI;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Snyk.VisualStudio.Extension.Settings
{
    [Guid("d45468c1-33d2-4dca-9780-68abaedf95e7")]
    public class SnykGeneralOptionsDialogPage : DialogPage, ISnykOptions
    {
        private SnykVSPackage package;

        protected override IWin32Window Window
        {
            get
            {
                var generalSettingsUserControl = new SnykGeneralSettingsUserControl(package.ActivityLogger);
                
                generalSettingsUserControl.optionsDialogPage = this;
                generalSettingsUserControl.Initialize();

                return generalSettingsUserControl;
            }
        }

        public void Initialize(SnykVSPackage package)
        {
            this.package = package;
        }

        public SnykVSPackage Package
        {
            get
            {
                return package;
            }
        }

        public string ApiToken { get; set; }

        public string CustomEndpoint { get; set; }

        public string Organization { get; set; }

        public bool IgnoreUnknownCA { get; set; }     

        public string AdditionalOptions
        {
            get
            {
                var settingsService = package.SolutionService.SolutionSettingsService;

                return settingsService.GetAdditionalOptions();
            }
        }        
    }
}
