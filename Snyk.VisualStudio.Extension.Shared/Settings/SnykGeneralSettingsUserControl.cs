namespace Snyk.VisualStudio.Extension.Shared.Settings
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Threading;
    using Serilog;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.CLI;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using Snyk.VisualStudio.Extension.Shared.UI.Notifications;
    using static Snyk.VisualStudio.Extension.Shared.CLI.Download.SnykCliDownloader;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Control for Snyk General Settings.
    /// </summary>
    public partial class SnykGeneralSettingsUserControl : UserControl
    {
        private static readonly ILogger logger = LogManager.ForContext<SnykGeneralSettingsUserControl>();

        /// <summary>
        /// Instance of SnykGeneralOptionsDialogPage.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed.")]
        internal SnykGeneralOptionsDialogPage OptionsDialogPage;

        private static readonly int TwoSecondsDelay = 2000;

        private static readonly int MaxSastRequestAttempts = 20;

        private ISnykApiService apiService;

        private Timer snykCodeEnableTimer = new Timer();

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
            logger.Information("Enter Initialize method");

            this.InitializeApiToken();
            this.UpdateViewFromOptionsDialog();
            this.OptionsDialogPage.SettingsChanged += this.OptionsDialogPageOnSettingsChanged;
            this.Load += this.SnykGeneralSettingsUserControl_Load;

            logger.Information("Leave Initialize method");
        }

        private void UpdateViewFromOptionsDialog()
        {
            this.customEndpointTextBox.Text = this.OptionsDialogPage.CustomEndpoint;
            this.organizationTextBox.Text = this.OptionsDialogPage.Organization;
            this.ignoreUnknownCACheckBox.Checked = this.OptionsDialogPage.IgnoreUnknownCA;
            this.usageAnalyticsCheckBox.Checked = this.OptionsDialogPage.UsageAnalyticsEnabled;
            this.ossEnabledCheckBox.Checked = this.OptionsDialogPage.OssEnabled;
            this.CliAutoUpdate.Checked = this.OptionsDialogPage.CliAutoUpdate;
            this.CliCustomPathTextBox.Text = this.OptionsDialogPage.CliCustomPath;
        }

        private void OptionsDialogPageOnSettingsChanged(object sender, SnykSettingsChangedEventArgs e)
        {
            this.UpdateViewFromOptionsDialog();
            this.InitializeApiToken();
        }

        /// <summary>
        /// Authenticate user via cli auth.
        /// </summary>
        /// <exception cref="FileNotFoundException">Thrown when the CLI could not be found.</exception>
        /// <returns>Returns true if authenticated successfully, false otherwise.</returns>
        public bool Authenticate()
        {
            logger.Information("Enter Authenticate method");

            var cli = this.OptionsDialogPage.ServiceProvider.NewCli();

            if (!cli.IsCliFileFound())
            {
                logger.Information("CLI not exists. Download CLI before get Api token");
                throw new FileNotFoundException("CLI was not found");
            }

            logger.Information("CLI exists. Calling SetupApiToken method");

            return this.SetupApiToken();
        }

        private async Task OnAuthenticationSuccessfulAsync(string apiToken)
        {
            logger.Information("Enter authenticate successCallback");

            //TODO - try to await SwitchToMainThread
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

            await this.OptionsDialogPage.ServiceProvider.ToolWindow.UpdateScreenStateAsync();
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

        private async Task OnAuthenticationFailAsync(string errorMessage)
        {
            logger.Information("Enter authenticate errorCallback");

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

            await this.OptionsDialogPage.ServiceProvider.ToolWindow.UpdateScreenStateAsync();
        }

        private SnykCli NewCli() => new SnykCli(this.OptionsDialogPage);

        private void AuthenticateButton_Click(object sender, EventArgs eventArgs) => ThreadHelper.JoinableTaskFactory
            .RunAsync(this.AuthenticateButtonClickAsync);

        private async Task AuthenticateButtonClickAsync()
        {
            logger.Information("Enter authenticateButton_Click method");

            this.authProgressBar.Visible = true;
            this.tokenTextBox.Enabled = false;
            this.authenticateButton.Enabled = false;

            logger.Information("Start run task");
            await TaskScheduler.Default;

            var serviceProvider = this.OptionsDialogPage.ServiceProvider;

            var cli = this.OptionsDialogPage.ServiceProvider.NewCli();
            if (cli.IsCliFileFound())
            {
                logger.Information("CLI exists. Calling SetupApiToken method");

                await this.SetupApiTokenAsync();
            }
            else
            {
                logger.Information("CLI not exists. Download CLI before get Api token");

                serviceProvider.TasksService.Download(this.OnCliDownloadFinishedCallback);
            }
        }

        private void OnCliDownloadFinishedCallback() => ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            logger.Information("CLI downloaded. Calling SetupApiToken method");

            await this.SetupApiTokenAsync();
        });

        private async Task SetupApiTokenAsync()
        {
            logger.Information("Enter SetupApiToken method");

            string apiToken;

            try
            {
                logger.Information("Try get Api token");

                apiToken = this.NewCli().GetApiToken();

                if (string.IsNullOrEmpty(apiToken))
                {
                    logger.Information("Api toke is null or empty. Try to authenticate via snyk auth");

                    string authResultMessage = this.NewCli().Authenticate();

                    if (authResultMessage.Contains("Your account has been authenticated. Snyk is now ready to be used."))
                    {
                        logger.Information("Snyk auth executed successfully. Try to get Api token");

                        apiToken = this.NewCli().GetApiToken();
                    }
                    else
                    {
                        logger.Information("Snyk auth executed with error: {AuthResultMessage}", authResultMessage);

                        await this.OnAuthenticationFailAsync(authResultMessage);

                        return;
                    }
                }

                logger.Information("Validate Api token GUID");

                if (!Common.Guid.IsValid(apiToken))
                {
                    await this.OnAuthenticationFailAsync($"Invalid GUID: {apiToken}");

                    return;
                }

                await this.OnAuthenticationSuccessfulAsync(apiToken);

                logger.Information("Leave SetupApiToken method");
            }
            catch (Exception e)
            {
                logger.Error(e, "Setup api token in general settings");

                await this.OnAuthenticationFailAsync(e.Message);
            }
        }

        private bool SetupApiToken()
        {
            logger.Information("Enter SetupApiToken method");
            try
            {
                logger.Information("Try get Api token");
                var apiToken = this.NewCli().GetApiToken();

                if (string.IsNullOrEmpty(apiToken))
                {
                    logger.Information("Api toke is null or empty. Try to authenticate via snyk auth");
                    string authResultMessage = this.NewCli().Authenticate();

                    if (authResultMessage.Contains("Your account has been authenticated. Snyk is now ready to be used."))
                    {
                        logger.Information("Snyk auth executed successfully. Try to get Api token");
                        apiToken = this.NewCli().GetApiToken();
                    }
                    else
                    {
                        logger.Information("Snyk auth executed with error: {AuthResultMessage}", authResultMessage);
                        NotificationService.Instance.ShowErrorInfoBar(authResultMessage);
                        return false;
                    }
                }

                logger.Information("Validate Api token GUID");

                if (!Common.Guid.IsValid(apiToken))
                {
                    NotificationService.Instance.ShowErrorInfoBar($"Invalid API Token: {apiToken}");
                    return false;
                }

                this.OptionsDialogPage.ApiToken = apiToken;
                return true;

            }
            catch (Exception e)
            {
                logger.Error(e, "Setup api token in general settings");
                return false;
            }
        }

        private bool IsValidUrl(string url) => Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute);

        private void TokenTextBox_TextChanged(object sender, EventArgs e)
        {
            this.ValidateChildren(ValidationConstraints.Enabled);

            this.OptionsDialogPage.ApiToken = this.tokenTextBox.Text;
        }

        private void CustomEndpointTextBox_LostFocus(object sender, EventArgs e)
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

        private void TokenTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs cancelEventArgs) =>
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await this.OptionsDialogPage.ServiceProvider.ToolWindow.UpdateScreenStateAsync();

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
            });

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

            bool snykCodeEnabled = sastSettings?.SnykCodeEnabled ?? false;

            this.codeSecurityEnabledCheckBox.Enabled = snykCodeEnabled;
            this.codeQualityEnabledCheckBox.Enabled = snykCodeEnabled;

            if (sastSettings?.LocalCodeEngineEnabled ?? false)
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

                this.snykCodeEnableTimer.Tick += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
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
                });

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

        private void UsageAnalyticsCheckBox_CheckedChanged(object sender, EventArgs e) =>
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                this.OptionsDialogPage.UsageAnalyticsEnabled = this.usageAnalyticsCheckBox.Checked;

                var serviceProvider = this.OptionsDialogPage.ServiceProvider;

                serviceProvider.AnalyticsService.AnalyticsEnabledOption = this.usageAnalyticsCheckBox.Checked;

                await serviceProvider.AnalyticsService.ObtainUserAsync(serviceProvider.GetApiToken());
                await serviceProvider.SentryService.SetupAsync();
            });

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

        private void OrganizationInfoLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.OrganizationInfoLink.LinkVisited = true;
            Process.Start("https://docs.snyk.io/ide-tools/visual-studio-extension#organization-setting");
        }

        private void CliAutoUpdate_CheckedChanged(object sender, EventArgs e)
        {
            this.OptionsDialogPage.CliAutoUpdate = this.CliAutoUpdate.Checked;
        }

        private void CliCustomPathBrowseButton_Click(object sender, EventArgs e)
        {
            if (this.customCliPathFileDialog.ShowDialog() == DialogResult.OK)
            {
                var selectedCliPath = this.customCliPathFileDialog.FileName;
                this.SetCliCustomPathValue(selectedCliPath);
            }
        }

        private void SetCliCustomPathValue(string selectedCliPath)
        {
            this.OptionsDialogPage.CliCustomPath = selectedCliPath;
            this.CliCustomPathTextBox.Text = selectedCliPath;
        }

        private void ClearCliCustomPathButton_Click(object sender, EventArgs e)
        {
            this.SetCliCustomPathValue(string.Empty);
        }
    }
}
