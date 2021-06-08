namespace Snyk.VisualStudio.Extension.Settings
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using Microsoft.VisualStudio.Shell;
    using Snyk.VisualStudio.Extension.Service;

    /// <summary>
    /// Snyk general settings page.
    /// </summary>
    [Guid("d45468c1-33d2-4dca-9780-68abaedf95e7")]
    public class SnykGeneralOptionsDialogPage : DialogPage, ISnykOptions
    {
        private ISnykServiceProvider serviceProvider;

        private SnykUserStorageSettingsService userStorageSettingsService;

        private SnykGeneralSettingsUserControl generalSettingsUserControl;

        private SnykActivityLogger logger;

        /// <summary>
        /// Gets a value indicating whether service provider.
        /// </summary>
        public ISnykServiceProvider ServiceProvider => this.serviceProvider;

        /// <summary>
        /// Gets a value indicating whether General Settings control.
        /// </summary>
        protected override IWin32Window Window => this.GeneralSettingsUserControl;

        /// <summary>
        /// Gets or sets a value indicating whether API token.
        /// </summary>
        public string ApiToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Custom endpoint.
        /// </summary>
        public string CustomEndpoint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether organization.
        /// </summary>
        public string Organization { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ignore unknown CA.
        /// </summary>
        public bool IgnoreUnknownCA { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether use analytics enabled.
        /// Save data via <see cref="SnykUserStorageSettingsService"/>.
        /// </summary>
        public bool UsageAnalyticsEnabled
        {
            get
            {
                return this.userStorageSettingsService.GetUsageAnalyticsEnabled();
            }

            set
            {
                try
                {
                    this.userStorageSettingsService?.SaveUsageAnalyticsEnabled(value);
                }
                catch (Exception exception)
                {
                    this.logger?.LogError(exception.Message);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether additional options.
        /// Get this data using <see cref="SnykUserStorageSettingsService"/>.
        /// </summary>
        public string AdditionalOptions => this.serviceProvider.UserStorageSettingsService.GetAdditionalOptions();

        /// <summary>
        /// Gets a value indicating whether is scan all projects enabled via <see cref="SnykUserStorageSettingsService"/>.
        /// Get this data using <see cref="SnykUserStorageSettingsService"/>.
        /// </summary>
        public bool IsScanAllProjects => this.userStorageSettingsService.GetIsAllProjectsEnabled();

        private SnykGeneralSettingsUserControl GeneralSettingsUserControl
        {
            get
            {
                if (this.generalSettingsUserControl == null)
                {
                    this.generalSettingsUserControl = new SnykGeneralSettingsUserControl(this.logger)
                    {
                        OptionsDialogPage = this,
                    };

                    this.generalSettingsUserControl.Initialize();
                }

                return this.generalSettingsUserControl;
            }
        }

        /// <summary>
        /// Initialize <see cref="SnykGeneralOptionsDialogPage"/>.
        /// </summary>
        /// <param name="provider">Snyk service provider.</param>
        public void Initialize(ISnykServiceProvider provider)
        {
            this.serviceProvider = provider;

            this.userStorageSettingsService = this.serviceProvider.UserStorageSettingsService;
            this.logger = this.serviceProvider.ActivityLogger;
        }

        /// <summary>
        /// Authenticate CLI.
        /// </summary>
        /// <param name="successCallbackAction">Success callback.</param>
        /// <param name="errorCallbackAction">Error callback.</param>
        public void Authenticate(Action<string> successCallbackAction, Action<string> errorCallbackAction)
            => this.GeneralSettingsUserControl.Authenticate(successCallbackAction, errorCallbackAction);
    }
}
