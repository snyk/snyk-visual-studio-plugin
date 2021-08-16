namespace Snyk.VisualStudio.Extension.Settings
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Snyk.VisualStudio.Extension.CLI;
    using static Snyk.VisualStudio.Extension.CLI.SnykCliDownloader;

    /// <summary>
    /// Control for Snyk General Settings.
    /// </summary>
    public partial class SnykGeneralSettingsUserControl : UserControl
    {
        /// <summary>
        /// Instance of SnykGeneralOptionsDialogPage.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed.")]
        internal SnykGeneralOptionsDialogPage OptionsDialogPage;

        private static readonly Regex GuidRegex = new Regex(@"^(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}$", RegexOptions.Compiled);

        private SnykActivityLogger logger;

        private Action<string> successCallbackAction;

        private Action<string> errorCallbackAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykGeneralSettingsUserControl"/> class.
        /// </summary>
        /// <param name="logger">ActivityLogger parameter.</param>
        public SnykGeneralSettingsUserControl(SnykActivityLogger logger)
        {
            this.InitializeComponent();

            this.logger = logger;
        }

        /// <summary>
        /// Initialize elements and actions.
        /// </summary>
        public void Initialize()
        {
            this.logger.LogInformation("Enter Initialize method");

            this.InitializeApiToken();

            this.customEndpointTextBox.Text = this.OptionsDialogPage.CustomEndpoint;
            this.organizationTextBox.Text = this.OptionsDialogPage.Organization;
            this.ignoreUnknownCACheckBox.Checked = this.OptionsDialogPage.IgnoreUnknownCA;
            this.usageAnalyticsCheckBox.Checked = this.OptionsDialogPage.UsageAnalyticsEnabled;
            this.ossEnabledCheckBox.Checked = this.OptionsDialogPage.OssEnabled;
            this.codeSecurityEnabledCheckBox.Checked = this.OptionsDialogPage.SnykCodeSecurityEnabled;
            this.codeQualityEnabledCheckBox.Checked = this.OptionsDialogPage.SnykCodeQualityEnabled;

            this.successCallbackAction = (apiToken) =>
            {
                this.logger.LogInformation("Enter authenticate successCallback");

                this.authProgressBar.Invoke((MethodInvoker)delegate
                {
                    this.authProgressBar.Visible = false;
                });

                this.tokenTextBox.Invoke((MethodInvoker)delegate
                {
                    this.tokenTextBox.Text = apiToken;
                    this.tokenTextBox.Enabled = true;
                });

                this.authenticateButton.Invoke((MethodInvoker)delegate
                {
                    this.authenticateButton.Enabled = true;
                });

                this.authenticateButton.Invoke((MethodInvoker)delegate
                {
                    this.errorProvider.SetError(this.tokenTextBox, string.Empty);
                });
            };

            this.errorCallbackAction = (errorMessage) =>
            {
                this.logger.LogInformation("Enter authenticate errorCallback");

                this.authProgressBar.Invoke((MethodInvoker)delegate
                {
                    this.authProgressBar.Visible = false;
                });

                this.tokenTextBox.Invoke((MethodInvoker)delegate
                {
                    this.tokenTextBox.Enabled = true;
                });

                this.authenticateButton.Invoke((MethodInvoker)delegate
                {
                    this.authenticateButton.Enabled = true;
                });

                CliError cliError = new CliError
                {
                    IsSuccess = false,
                    Message = errorMessage,
                    Path = string.Empty,
                };

                this.OptionsDialogPage.ServiceProvider.TasksService.OnCliError(cliError);

                this.OptionsDialogPage.ServiceProvider.ShowToolWindow();
            };

            this.logger.LogInformation("Leave Initialize method");
        }

        /// <summary>
        /// Authenticate user via cli auth.
        /// </summary>
        /// <param name="successCallbackAction">Callback for success authentication.</param>
        /// <param name="errorCallbackAction">Callback for fail authentication.</param>
        public void Authenticate(Action<string> successCallbackAction, Action<string> errorCallbackAction)
        {
            this.logger.LogInformation("Enter Authenticate method");

            _ = Task.Run(() =>
            {
                var serviceProvider = this.OptionsDialogPage.ServiceProvider;
                var tasksService = serviceProvider.TasksService;

                if (SnykCli.IsCliExists())
                {
                    this.logger.LogInformation("CLI exists. Calling SetupApiToken method");

                    this.SetupApiToken(successCallbackAction, errorCallbackAction);
                }
                else
                {
                    this.logger.LogInformation("CLI not exists. Download CLI before get Api token");

                    serviceProvider.TasksService.Download(new CliDownloadFinishedCallback(this.OnCliDownloadFinishedCallback));
                }
            });

            this.logger.LogInformation("Leave Authenticate method");
        }

        private void InitializeApiToken()
        {
            if (string.IsNullOrEmpty(this.OptionsDialogPage.ApiToken))
            {
                string apiToken = this.NewCli().GetApiToken();

                if (this.IsValidGuid(apiToken))
                {
                    this.OptionsDialogPage.ApiToken = apiToken;
                }
            }

            this.tokenTextBox.Text = this.OptionsDialogPage.ApiToken;
        }

        private SnykCli NewCli() => new SnykCli
        {
            Options = this.OptionsDialogPage,
        };

        private void AuthenticateButton_Click(object sender, EventArgs eventArgs)
        {
            this.logger.LogInformation("Enter authenticateButton_Click method");

            this.authProgressBar.Visible = true;
            this.tokenTextBox.Enabled = false;
            this.authenticateButton.Enabled = false;

            this.logger.LogInformation("Start run task");

            Task.Run(() =>
            {
                var serviceProvider = OptionsDialogPage.ServiceProvider;
                var tasksService = serviceProvider.TasksService;

                if (SnykCli.IsCliExists())
                {
                    this.logger.LogInformation("CLI exists. Calling SetupApiToken method");

                    this.SetupApiToken(this.successCallbackAction, this.errorCallbackAction);
                }
                else
                {
                    this.logger.LogInformation("CLI not exists. Download CLI before get Api token");

                    serviceProvider.TasksService.Download(new CliDownloadFinishedCallback(this.OnCliDownloadFinishedCallback));
                }
            });
        }

        private void OnCliDownloadFinishedCallback()
        {
            this.logger.LogInformation("CLI downloaded. Calling SetupApiToken method");

            this.SetupApiToken(this.successCallbackAction, this.errorCallbackAction);
        }

        private void SetupApiToken(Action<string> successCallback, Action<string> errorCallback)
        {
            this.logger.LogInformation("Enter SetupApiToken method");

            var cli = this.NewCli();

            string apiToken = string.Empty;

            try
            {
                this.logger.LogInformation("Try get Api toke");

                apiToken = cli.GetApiToken();

                if (string.IsNullOrEmpty(apiToken))
                {
                    this.logger.LogInformation("Api toke is null or empty. Try to authenticate via snyk auth");

                    string authResultMessage = cli.Authenticate();

                    if (authResultMessage.Contains("Your account has been authenticated. Snyk is now ready to be used."))
                    {
                        this.logger.LogInformation("Snyk auth executed successfully. Try to get Api token");

                        apiToken = cli.GetApiToken();
                    }
                    else
                    {
                        this.logger.LogInformation($"Snyk auth executed with error: {authResultMessage}");

                        errorCallback(authResultMessage);

                        return;
                    }
                }

                this.logger.LogInformation("Validate Api token GUID");

                if (!this.IsValidGuid(apiToken))
                {
                    errorCallback($"Invalid GUID: {apiToken}");

                    return;
                }

                successCallback(apiToken);

                this.logger.LogInformation("Leave SetupApiToken method");
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception.Message);

                errorCallback(exception.Message);
            }
        }

        private bool IsValidGuid(string guid) => guid != null && GuidRegex.IsMatch(guid);

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
            if (string.IsNullOrEmpty(this.tokenTextBox.Text))
            {
                this.errorProvider.SetError(this.tokenTextBox, string.Empty);

                return;
            }

            if (string.IsNullOrWhiteSpace(this.tokenTextBox.Text) || !this.IsValidGuid(this.tokenTextBox.Text))
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
        }

        private void UsageAnalyticsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.OptionsDialogPage.UsageAnalyticsEnabled = this.usageAnalyticsCheckBox.Checked;

            this.OptionsDialogPage.ServiceProvider.AnalyticsService.AnalyticsEnabled = this.usageAnalyticsCheckBox.Checked;

            this.OptionsDialogPage.ServiceProvider.AnalyticsService.ObtainUser(this.OptionsDialogPage.ServiceProvider.GetApiToken());
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
    }
}
