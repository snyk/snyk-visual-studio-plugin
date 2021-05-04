using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Service;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Snyk.VisualStudio.Extension.Settings
{
    [Guid("d45468c1-33d2-4dca-9780-68abaedf95e7")]
    public class SnykGeneralOptionsDialogPage : DialogPage, ISnykOptions
    {
        private ISnykServiceProvider serviceProvider;

        private SnykGeneralSettingsUserControl generalSettingsUserControl;
        
        protected override IWin32Window Window => GeneralSettingsUserControl;

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

            var settingsService = serviceProvider.SolutionService.SolutionSettingsService;
        }

        public void Authenticate(Action<string> successCallbackAction, Action<string> errorCallbackAction)
            => GeneralSettingsUserControl.Authenticate(successCallbackAction, errorCallbackAction);

        public string ApiToken { get; set; }

        public string CustomEndpoint { get; set; }

        public string Organization { get; set; }

        public bool IgnoreUnknownCA { get; set; }

        public bool UsageAnalyticsEnabled
        {
            get
            {
                var settingsService = serviceProvider.SolutionService.SolutionSettingsService;

                return settingsService.GetUsageAnalyticsEnabled();
            }

            set
            {
                try
                {
                    var settingsService = serviceProvider?.SolutionService.SolutionSettingsService;

                    settingsService?.SaveUsageAnalyticsEnabled(value);
                }
                catch (Exception exception)
                {
                    serviceProvider?.ActivityLogger?.LogError(exception.Message);    
                }
            }
        }

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

        private SnykGeneralSettingsUserControl GeneralSettingsUserControl
        {
            get
            {
                if (generalSettingsUserControl == null)
                {
                    generalSettingsUserControl = new SnykGeneralSettingsUserControl(serviceProvider.ActivityLogger)
                    {
                        optionsDialogPage = this
                    };

                    generalSettingsUserControl.Initialize();
                }

                return generalSettingsUserControl;
            }
        }
    }
}
