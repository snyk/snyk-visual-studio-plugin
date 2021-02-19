using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Service;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Snyk.VisualStudio.Extension.Settings
{
    [Guid("d45468c1-33d2-4dca-9780-68abaedf95e7")]
    public class SnykGeneralOptionsDialogPage : DialogPage, ISnykOptions
    {
        private ISnykServiceProvider serviceProvider;

        protected override IWin32Window Window
        {
            get
            {
                var generalSettingsUserControl = new SnykGeneralSettingsUserControl(serviceProvider.ActivityLogger);
                
                generalSettingsUserControl.optionsDialogPage = this;
                generalSettingsUserControl.Initialize();

                return generalSettingsUserControl;
            }
        }        

        public ISnykServiceProvider ServiceProvider
        {
            get
            {
                return serviceProvider;
            }
        }

        public void Initialize(ISnykServiceProvider provider)
        {
            this.serviceProvider = provider;
        }

        public string ApiToken { get; set; }

        public string CustomEndpoint { get; set; }

        public string Organization { get; set; }

        public bool IgnoreUnknownCA { get; set; }     

        public string AdditionalOptions
        {
            get
            {
                var settingsService = serviceProvider.SolutionService.SolutionSettingsService;

                return settingsService.GetAdditionalOptions();
            }
        }      
        
        public bool IsScanAllProjects
        {
            get
            {
                return serviceProvider.SolutionService.SolutionSettingsService.GetIsAllProjectsEnabled();
            }
        }              
    }
}
