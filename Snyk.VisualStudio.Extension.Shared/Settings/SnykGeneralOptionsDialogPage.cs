﻿namespace Snyk.VisualStudio.Extension.Shared.Settings
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using Microsoft.VisualStudio.Shell;
    using Serilog;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.Service;

    /// <summary>
    /// Snyk general settings page.
    /// </summary>
    [Guid("d45468c1-33d2-4dca-9780-68abaedf95e7")]
    [ComVisible(true)]
    public class SnykGeneralOptionsDialogPage : DialogPage, ISnykOptions
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykGeneralOptionsDialogPage>();

        private ISnykServiceProvider serviceProvider;

        private SnykUserStorageSettingsService userStorageSettingsService;

        private SnykGeneralSettingsUserControl generalSettingsUserControl;

        private string apiToken;

        private string customEndpoint;

        /// <inheritdoc/>
        public event EventHandler<SnykSettingsChangedEventArgs> SettingsChanged;

        /// <summary>
        /// Gets a value indicating whether service provider.
        /// </summary>
        public ISnykServiceProvider ServiceProvider => this.serviceProvider;

        /// <summary>
        /// Gets or sets a value indicating whether API token.
        /// </summary>
        public string ApiToken
        {
            get => this.apiToken;
            set
            {
                this.apiToken = value;

                this.FireSettingsChangedEvent();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Custom endpoint.
        /// </summary>
        public string CustomEndpoint
        {
            get => this.customEndpoint;
            set
            {
                this.customEndpoint = value;

                this.FireSettingsChangedEvent();
            }
        }

        /// <inheritdoc/>
        public string SnykCodeSettingsUrl => $"{this.GetAppCustomEndpoint()}/manage/snyk-code";


        /// <summary>
        /// Gets or sets a value indicating whether organization.
        /// </summary>
        public string Organization { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ignore unknown CA.
        /// </summary>
        public bool IgnoreUnknownCA { get; set; }

        /// <inheritdoc/>
        public bool OssEnabled
        {
            get => this.userStorageSettingsService.IsOssEnabled();
            set
            {
                this.userStorageSettingsService?.SaveOssEnabled(value);

                this.FireSettingsChangedEvent();
            }
        }

        /// <inheritdoc/>
        public bool SnykCodeSecurityEnabled
        {
            get => this.userStorageSettingsService.IsSnykCodeSecurityEnabled();
            set
            {
                this.userStorageSettingsService?.SaveSnykCodeSecurityEnabled(value);

                this.FireSettingsChangedEvent();
            }
        }

        /// <inheritdoc/>
        public bool SnykCodeQualityEnabled
        {
            get => this.userStorageSettingsService.IsSnykCodeQualityEnabled();
            set
            {
                this.userStorageSettingsService?.SaveSnykCodeQualityEnabled(value);

                this.FireSettingsChangedEvent();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether use analytics enabled.
        /// Save data via <see cref="SnykUserStorageSettingsService"/>.
        /// </summary>
        public bool UsageAnalyticsEnabled
        {
            get => this.userStorageSettingsService.IsUsageAnalyticsEnabled();
            set => this.userStorageSettingsService?.SaveUsageAnalyticsEnabled(value);
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

        /// <inheritdoc/>
        public string AnonymousId
        {
            get => this.userStorageSettingsService.GetAnonymousId();
            set => this.userStorageSettingsService?.SaveAnonymousId(value);
        }

        /// <summary>
        /// Gets a value indicating whether General Settings control.
        /// </summary>
        protected override IWin32Window Window => this.GeneralSettingsUserControl;

        private SnykGeneralSettingsUserControl GeneralSettingsUserControl
        {
            get
            {
                if (this.generalSettingsUserControl == null)
                {
                    this.generalSettingsUserControl = new SnykGeneralSettingsUserControl(this.serviceProvider.ApiService)
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
        }

        /// <summary>
        /// Authenticate CLI.
        /// </summary>
        /// <param name="successCallbackAction">Success callback.</param>
        /// <param name="errorCallbackAction">Error callback.</param>
        public void Authenticate(Action<string> successCallbackAction, Action<string> errorCallbackAction)
            => this.GeneralSettingsUserControl.Authenticate(successCallbackAction, errorCallbackAction);

        private void FireSettingsChangedEvent() => this.SettingsChanged?.Invoke(this, new SnykSettingsChangedEventArgs());

        private string GetAppCustomEndpoint() => string.IsNullOrEmpty(this.customEndpoint)
            ? "https://app.snyk.io"
            : this.customEndpoint
                .Replace("https://", "https://app.")
                .Replace("/api", string.Empty);
    }
}
