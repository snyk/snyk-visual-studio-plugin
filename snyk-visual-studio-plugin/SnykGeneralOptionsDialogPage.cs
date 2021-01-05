using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.UI;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Snyk.VisualStudio.Extension.Settings
{
    [Guid("d45468c1-33d2-4dca-9780-68abaedf95e7")]
    public class SnykGeneralOptionsDialogPage : DialogPage, ISnykOptions
    {
        private string apiToken = "";
        private string customEndpoint = "";
        private string organization = "";
        private bool ignoreUnknownCA = false;
        private string additionalOptions = "";

        private SnykVSPackage package;

        protected override IWin32Window Window
        {
            get
            {
                var generalSettingsUserControl = new SnykGeneralSettingsUserControl();
                
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

        public string ApiToken
        {
            get
            {
                return apiToken;
            }
            set
            {
                apiToken = value;
            }
        }

        public string CustomEndpoint
        {
            get
            {
                return customEndpoint;
            }
            set
            {
                customEndpoint = value;
            }
        }

        public string Organization
        {
            get
            {
                return organization;
            }
            set
            {
                organization = value;
            }
        }

        public bool IgnoreUnknownCA
        {
            get
            {
                return ignoreUnknownCA;
            }
            set
            {
                ignoreUnknownCA = value;
            }
        }        

        public string AdditionalOptions
        {
            get
            {
                return additionalOptions;
            }

            set
            {
                additionalOptions = value;
            }
        }        
    }
}
