namespace Snyk.VisualStudio.Extension.Shared.Settings
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Serilog;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.CLI;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using Snyk.VisualStudio.Extension.Shared.UI.Notifications;
    using static Snyk.VisualStudio.Extension.Shared.CLI.Download.SnykCliDownloader;

    /// <summary>
    /// Control for Snyk General Settings.
    /// </summary>
    public partial class SnykGeneralSettingsUserControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykGeneralSettingsUserControl>();

        /// <summary>
        /// Instance of SnykGeneralOptionsDialogPage.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed.")]
        internal SnykGeneralOptionsDialogPage OptionsDialogPage;

        private static readonly int TwoSecondsDelay = 2000;

        private static readonly int MaxSastRequestAttempts = 20;

        private ISnykApiService apiService;

        private Timer snykCodeEnableTimer = new Timer();

        private Action<string> successCallbackAction;

        private Action<string> errorCallbackAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykGeneralSettingsUserControl"/> class.
        /// </summary>
        /// <param name="apiService">Snyk API service instance.</param>
        public SnykGeneralSettingsUserControl(ISnykApiService apiService)
        {
            this.InitializeComponent();

            this.apiService = apiService;
        }

        /// <summary>
        /// Initialize elements and actions.
        /// </summary>
        public void Initialize()
        {
            Logger.Information("Enter Initialize method");

            this.InitializeApiToken();

            this.customEndpointTextBox.Text = this.OptionsDialogPage.CustomEndpoint;
            this.organizationTextBox.Text = this.OptionsDialogPage.Organization;
            this.ignoreUnknownCACheckBox.Checked = this.OptionsDialogPage.IgnoreUnknownCA;
            this.usageAnalyticsCheckBox.Checked = this.OptionsDialogPage.UsageAnalyticsEnabled;
            this.ossEnabledCheckBox.Checked = this.OptionsDialogPage.OssEnabled;

            this.successCallbackAction = (apiToken) =>
            {
                Logger.Information("Enter authenticate successCallback");

                if (this.authProgressBar.IsHandleCreated)
                {
                    this.authProgressBar.Invoke(new Action(() =>
                    {
                        this.authProgressBar.Visible = false;
                    }));
                }

                if (this.tokenTextBox.IsHandleCreated)
                {
                    this.tokenTextBox.Invoke(new Action(() =>
                    {
                        this.tokenTextBox.Text = apiToken;
                        this.tokenTextBox.Enabled = true;
                    }));
                }

                if (this.authenticateButton.IsHandleCreated)
                {
                    this.authenticateButton.Invoke(new Action(() =>
                    {
                        this.authenticateButton.Enabled = true;
                    }));
                }

                if (this.authenticateButton.IsHandleCreated)
                {
                    this.authenticateButton.Invoke(new Action(() =>
                    {
                        this.errorProvider.SetError(this.tokenTextBox, string.Empty);
                    }));
                }

                this.OptionsDialogPage.ServiceProvider.ToolWindow.UpdateScreenState();
            };

            this.errorCallbackAction = (errorMessage) =>
            {
                Logger.Information("Enter authenticate errorCallback");

                if (this.authProgressBar.IsHandleCreated)
                {
                    this.authProgressBar.Invoke(new Action(() =>
                    {
                        this.authProgressBar.Visible = false;
                    }));
                }

                if (this.tokenTextBox.IsHandleCreated)
                {
                    this.tokenTextBox.Invoke(new Action(() =>
                    {
                        this.tokenTextBox.Enabled = true;
                    }));
                }

                if (this.authenticateButton.IsHandleCreated)
                {
                    this.authenticateButton.Invoke(new Action(() =>
                    {
                        this.authenticateButton.Enabled = true;
                    }));
                }

                CliError cliError = new CliError
                {
                    IsSuccess = false,
                    Message = errorMessage,
                    Path = string.Empty,
                };

                this.OptionsDialogPage.ServiceProvider.TasksService.FireOssError(cliError);

                this.OptionsDialogPage.ServiceProvider.ToolWindow.Show();

                this.OptionsDialogPage.ServiceProvider.ToolWindow.UpdateScreenState();
            };

            Logger.Information("Leave Initialize method");
        }

        /// <summary>
        /// Authenticate user via cli auth.
        /// </summary>
        /// <param name="successCallbackAction">Callback for success authentication.</param>
        /// <param name="errorCallbackAction">Callback for fail authentication.</param>
        public void Authenticate(Action<string> successCallbackAction, Action<string> errorCallbackAction)
        {
            Logger.Information("Enter Authenticate method");

            _ = Task.Run(() =>
            {
                var serviceProvider = this.OptionsDialogPage.ServiceProvider;
                var tasksService = serviceProvider.TasksService;

                if (SnykCli.IsCliExists())
                {
                    Logger.Information("CLI exists. Calling SetupApiToken method");

                    this.SetupApiToken(successCallbackAction, errorCallbackAction);
                }
                else
                {
                    Logger.Information("CLI not exists. Download CLI before get Api token");

                    serviceProvider.TasksService.Download(new CliDownloadFinishedCallback(this.OnCliDownloadFinishedCallback));
                }
            });

            Logger.Information("Leave Authenticate method");
        }

        private void InitializeApiToken()
        {
            if (string.IsNullOrEmpty(this.OptionsDialogPage.ApiToken))
            {
                string apiToken = this.NewCli().GetApiToken();

                if (Common.Guid.IsValid(apiToken))
                {
                    this.OptionsDialogPage.ApiToken = apiToken;
                }
            }

            this.tokenTextBox.Text = this.OptionsDialogPage.ApiToken;
        }

        private SnykCli NewCli() => new SnykCli { Options = this.OptionsDialogPage, };

        private void AuthenticateButton_Click(object sender, EventArgs eventArgs)
        {
            Logger.Information("Enter authenticateButton_Click method");

            this.authProgressBar.Visible = true;
            this.tokenTextBox.Enabled = false;
            this.authenticateButton.Enabled = false;

            Logger.Information("Start run task");

            Task.Run(() =>
            {
                var serviceProvider = this.OptionsDialogPage.ServiceProvider;
                var tasksService = serviceProvider.TasksService;

                if (SnykCli.IsCliExists())
                {
                    Logger.Information("CLI exists. Calling SetupApiToken method");

                    this.SetupApiToken(this.successCallbackAction, this.errorCallbackAction);
                }
                else
                {
                    Logger.Information("CLI not exists. Download CLI before get Api token");

                    serviceProvider.TasksService.Download(new CliDownloadFinishedCallback(this.OnCliDownloadFinishedCallback));
                }
            });
        }

        private void OnCliDownloadFinishedCallback()
        {
            Logger.Information("CLI downloaded. Calling SetupApiToken method");

            this.SetupApiToken(this.successCallbackAction, this.errorCallbackAction);
        }

        private void SetupApiToken(Action<string> successCallback, Action<string> errorCallback)
        {
            Logger.Information("Enter SetupApiToken method");

            string apiToken;

            try
            {
                Logger.Information("Try get Api token");

                apiToken = this.NewCli().GetApiToken();

                if (string.IsNullOrEmpty(apiToken))
                {
                    Logger.Information("Api toke is null or empty. Try to authenticate via snyk auth");

                    string authResultMessage = this.NewCli().Authenticate();

                    if (authResultMessage.Contains("Your account has been authenticated. Snyk is now ready to be used."))
                    {
                        Logger.Information("Snyk auth executed successfully. Try to get Api token");

                        apiToken = this.NewCli().GetApiToken();
                    }
                    else
                    {
                        Logger.Information("Snyk auth executed with error: {AuthResultMessage}", authResultMessage);

                        errorCallback(authResultMessage);

                        return;
                    }
                }

                Logger.Information("Validate Api token GUID");

                if (!Common.Guid.IsValid(apiToken))
                {
                    errorCallback($"Invalid GUID: {apiToken}");

                    return;
                }

                successCallback(apiToken);

                Logger.Information("Leave SetupApiToken method");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Setup api token in general settings");

                errorCallback(e.Message);
            }
        }

        private bool IsValidUrl(string url) => Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute);

        private void TokenTextBox_TextChanged(object sender, EventArgs e)
        {
            this.ValidateChildren(ValidationConstraints.Enabled);

            this.OptionsDialogPage.ApiToken = this.tokenTextBox.Text;
        }

        private void CustomEndpointTextBox_TextChanged(object sender, EventArgs e)
        {
            this.ValidateChildren(ValidationConstraints.Enabled);

            this.OptionsDialogPage.CustomEndpoint = this.customEndpointTextBox.Text;
        }

        private void OrganizationTextBox_TextChanged(object sender, EventArgs e)
        {
            this.OptionsDialogPage.Organization = this.organizationTextBox.Text;
        }

        private void IgnoreUnknownCACheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.OptionsDialogPage.IgnoreUnknownCA = this.ignoreUnknownCACheckBox.Checked;
        }

        private void TokenTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs cancelEventArgs)
        {
            this.OptionsDialogPage.ServiceProvider.ToolWindow.UpdateScreenState();

            if (string.IsNullOrEmpty(this.tokenTextBox.Text))
            {
                this.errorProvider.SetError(this.tokenTextBox, string.Empty);

                return;
            }

            if (!Common.Guid.IsValid(this.tokenTextBox.Text))
            {
                cancelEventArgs.Cancel = true;

                this.tokenTextBox.Focus();

                this.errorProvider.SetError(this.tokenTextBox, "Not valid GUID.");
            }
            else
            {
                cancelEventArgs.Cancel = false;
                this.errorProvider.SetError(this.tokenTextBox, string.Empty);
            }
        }

        private void CustomEndpointTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs cancelEventArgs)
        {
            if (string.IsNullOrEmpty(this.tokenTextBox.Text))
            {
                this.errorProvider.SetError(this.tokenTextBox, string.Empty);

                return;
            }

            if (!string.IsNullOrWhiteSpace(this.customEndpointTextBox.Text) && !this.IsValidUrl(this.customEndpointTextBox.Text))
            {
                cancelEventArgs.Cancel = true;

                this.customEndpointTextBox.Focus();

                this.errorProvider.SetError(this.customEndpointTextBox, "Not valid URL.");
            }
            else
            {
                cancelEventArgs.Cancel = false;

                this.errorProvider.SetError(this.customEndpointTextBox, string.Empty);
            }
        }

        private void SnykGeneralSettingsUserControl_Load(object sender, EventArgs e)
        {
            this.InitializeApiToken();

            _ = this.StartSastEnablementCheckLoopAsync();
        }

        private void UpdateSnykCodeEnablementSettings(SastSettings sastSettings)
        {
            if (sastSettings == null)
            {
                this.snykCodeDisabledInfoLabel.Text = "Snyk Code is disabled.";

                this.snykCodeDisabledInfoLabel.Visible = true;
                this.snykCodeSettingsLinkLabel.Visible = true;
                this.checkAgainLinkLabel.Visible = true;
            }

            bool snykCodeEnabled = sastSettings.SnykCodeEnabled;

            this.codeSecurityEnabledCheckBox.Enabled = snykCodeEnabled;
            this.codeQualityEnabledCheckBox.Enabled = snykCodeEnabled;

            if (sastSettings.LocalCodeEngineEnabled)
            {
                this.snykCodeDisabledInfoLabel.Text =
                    "Snyk Code is configured to use a Local Code Engine instance. This setup is not yet supported by the extension.";

                this.snykCodeDisabledInfoLabel.Visible = true;
                this.snykCodeSettingsLinkLabel.Visible = false;
                this.checkAgainLinkLabel.Visible = false;
            }
            else
            {
                this.snykCodeDisabledInfoLabel.Text = "Snyk Code is disabled by your organisation\'s configuration:";

                this.snykCodeDisabledInfoLabel.Visible = !snykCodeEnabled;
                this.snykCodeSettingsLinkLabel.Visible = !snykCodeEnabled;
                this.checkAgainLinkLabel.Visible = !snykCodeEnabled;
            }
        }

        private async Task StartSastEnablementCheckLoopAsync()
        {
            try
            {
                if (this.snykCodeEnableTimer.Enabled)
                {
                    this.snykCodeEnableTimer.Stop();
                }

                var sastSettings = await this.apiService.GetSastSettingsAsync();

                this.UpdateSnykCodeEnablementSettings(sastSettings);

                if (sastSettings != null && sastSettings.SastEnabled)
                {
                    return;
                }

                int currentRequestAttempt = 1;

                this.snykCodeEnableTimer.Interval = TwoSecondsDelay;

                this.snykCodeEnableTimer.Tick += async (sender, eventArgs) =>
                {
                    try
                    {
                        sastSettings = await this.apiService.GetSastSettingsAsync();

                        bool snykCodeEnabled = sastSettings != null ? sastSettings.SnykCodeEnabled : false;

                        this.UpdateSnykCodeEnablementSettings(sastSettings);

                        if (snykCodeEnabled)
                        {
                            this.snykCodeEnableTimer.Stop();
                        }
                        else if (currentRequestAttempt < MaxSastRequestAttempts)
                        {
                            currentRequestAttempt++;

                            this.snykCodeEnableTimer.Interval = TwoSecondsDelay * currentRequestAttempt;
                        }
                        else
                        {
                            this.snykCodeEnableTimer.Stop();
                        }
                    }
                    catch (Exception e)
                    {
                        this.HandleSastError(e);
                    }
                };

                this.snykCodeEnableTimer.Start();
            }
            catch (Exception e)
            {
                this.HandleSastError(e);
            }
        }

        private void HandleSastError(Exception e)
        {
            this.snykCodeEnableTimer.Stop();

            NotificationService.Instance.ShowErrorInfoBar(e.Message);

            this.codeSecurityEnabledCheckBox.Enabled = false;
            this.codeQualityEnabledCheckBox.Enabled = false;

            this.snykCodeDisabledInfoLabel.Visible = false;
            this.snykCodeSettingsLinkLabel.Visible = false;
            this.checkAgainLinkLabel.Visible = false;
        }

        private void UsageAnalyticsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.OptionsDialogPage.UsageAnalyticsEnabled = this.usageAnalyticsCheckBox.Checked;

            var serviceProvider = this.OptionsDialogPage.ServiceProvider;

            serviceProvider.AnalyticsService.AnalyticsEnabled = this.usageAnalyticsCheckBox.Checked;

            serviceProvider.AnalyticsService
                .ObtainUser(serviceProvider, () => serviceProvider.SentryService.SetupAsync());
        }

        private void OssEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.OptionsDialogPage.OssEnabled = this.ossEnabledCheckBox.Checked;
        }

        private void CodeSecurityEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.OptionsDialogPage.SnykCodeSecurityEnabled = this.codeSecurityEnabledCheckBox.Checked;
        }

        private void CodeQualityEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.OptionsDialogPage.SnykCodeQualityEnabled = this.codeQualityEnabledCheckBox.Checked;
        }

        private void SnykCodeSettingsLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            => System.Diagnostics.Process.Start(this.OptionsDialogPage.SnykCodeSettingsUrl);

        private void CheckAgainLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            _ = this.StartSastEnablementCheckLoopAsync();
        }
    }
}
