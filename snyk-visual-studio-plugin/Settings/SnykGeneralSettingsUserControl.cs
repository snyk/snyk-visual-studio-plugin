using System;
using System.Windows.Forms;
using Snyk.VisualStudio.Extension.CLI;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Snyk.VisualStudio.Extension.CLI.SnykCliDownloader;

namespace Snyk.VisualStudio.Extension.Settings
{   
    public partial class SnykGeneralSettingsUserControl : UserControl
    {
        private static Regex GuidRegex = new Regex(@"^(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}$", RegexOptions.Compiled);

        internal SnykGeneralOptionsDialogPage optionsDialogPage;

        private SnykActivityLogger logger;

        private Action<string> successCallbackAction;
        
        private Action<string> errorCallbackAction;

        public SnykGeneralSettingsUserControl(SnykActivityLogger logger)
        {
            InitializeComponent();

            this.logger = logger;
        }
        
        public void Initialize()
        {
            logger.LogInformation("Enter Initialize method");

            InitializeApiToken();

            customEndpointTextBox.Text = optionsDialogPage.CustomEndpoint;
            organizationTextBox.Text = optionsDialogPage.Organization;
            ignoreUnknownCACheckBox.Checked = optionsDialogPage.IgnoreUnknownCA;
            usageAnalyticsCheckBox.Checked = optionsDialogPage.UsageAnalyticsEnabled;

            successCallbackAction = (apiToken) =>
            {
                logger.LogInformation("Enter authenticate successCallback");

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
                    errorProvider.SetError(tokenTextBox, "");
                });
            };

            errorCallbackAction = (errorMessage) =>
            {
                logger.LogInformation("Enter authenticate errorCallback");

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
                    Path = ""
                };

                optionsDialogPage.ServiceProvider.TasksService.OnError(cliError);

                optionsDialogPage.ServiceProvider.ShowToolWindow();
            };

            logger.LogInformation("Leave Initialize method");
        }        

        public void Authenticate(Action<string> successCallbackAction, Action<string> errorCallbackAction)
        {
            logger.LogInformation("Enter Authenticate method");

            Task.Run(() =>
            {
                var serviceProvider = optionsDialogPage.ServiceProvider;
                var tasksService = serviceProvider.TasksService;

                if (SnykCli.IsCliExists())
                {
                    logger.LogInformation("CLI exists. Calling SetupApiToken method");

                    SetupApiToken(successCallbackAction, errorCallbackAction);
                }
                else
                {
                    logger.LogInformation("CLI not exists. Download CLI before get Api token");

                    serviceProvider.TasksService.Download(new CliDownloadFinishedCallback(OnCliDownloadFinishedCallback));
                }
            });

            logger.LogInformation("Leave Authenticate method");
        }

        private void InitializeApiToken()
        {            
            if (String.IsNullOrEmpty(optionsDialogPage.ApiToken))
            {
                string apiToken = NewCli().GetApiToken();
                
                if (IsValidGuid(apiToken))
                {
                    optionsDialogPage.ApiToken = apiToken;
                }                               
            }

            tokenTextBox.Text = optionsDialogPage.ApiToken;
        }

        private SnykCli NewCli() => new SnykCli 
        {
            Options = optionsDialogPage
        };

        private void authenticateButton_Click(object sender, EventArgs eventArgs)            
        {
            logger.LogInformation("Enter authenticateButton_Click method");

            this.authProgressBar.Visible = true;
            this.tokenTextBox.Enabled = false;
            this.authenticateButton.Enabled = false;

            logger.LogInformation("Start run task");

            Task.Run(() =>
            {
                var serviceProvider = optionsDialogPage.ServiceProvider;
                var tasksService = serviceProvider.TasksService;                

                if (SnykCli.IsCliExists())
                {
                    logger.LogInformation("CLI exists. Calling SetupApiToken method");

                    SetupApiToken(successCallbackAction, errorCallbackAction);
                }
                else
                {
                    logger.LogInformation("CLI not exists. Download CLI before get Api token");

                    serviceProvider.TasksService.Download(new CliDownloadFinishedCallback(OnCliDownloadFinishedCallback));
                }
            });            
        }

        private void OnCliDownloadFinishedCallback()
        {
            logger.LogInformation("CLI downloaded. Calling SetupApiToken method");

            SetupApiToken(successCallbackAction, errorCallbackAction);
        }

        private void SetupApiToken(Action<string> successCallback, Action<string> errorCallback)
        {
            logger.LogInformation("Enter SetupApiToken method");

            var cli = NewCli();

            string apiToken = "";

            try
            {
                logger.LogInformation("Try get Api toke");

                apiToken = cli.GetApiToken();

                if (String.IsNullOrEmpty(apiToken))
                {
                    logger.LogInformation("Api toke is null or empty. Try to authenticate via snyk auth");

                    string authResultMessage = cli.Authenticate();

                    if (authResultMessage.Contains("Your account has been authenticated. Snyk is now ready to be used."))
                    {
                        logger.LogInformation("Snyk auth executed successfully. Try to get Api token");

                        apiToken = cli.GetApiToken();
                    }
                    else
                    {
                        logger.LogInformation($"Snyk auth executed with error: {authResultMessage}");

                        errorCallback(authResultMessage);

                        return;
                    }
                }

                logger.LogInformation("Validate Api token GUID");

                if (!IsValidGuid(apiToken))
                {
                    errorCallback($"Invalid GUID: {apiToken}");

                    return;
                }

                successCallback(apiToken);

                logger.LogInformation("Leave SetupApiToken method");
            } catch (Exception exception)
            {
                logger.LogError(exception.Message);

                errorCallback(exception.Message);
            }                                    
        }

        private bool IsValidGuid(string guid) => guid != null && GuidRegex.IsMatch(guid);

        private bool IsValidUrl(string url) => Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute);

        private void tokenTextBox_TextChanged(object sender, EventArgs e)
        {
            ValidateChildren(ValidationConstraints.Enabled);

            optionsDialogPage.ApiToken = tokenTextBox.Text;
        }

        private void customEndpointTextBox_TextChanged(object sender, EventArgs e)
        {
            ValidateChildren(ValidationConstraints.Enabled);

            optionsDialogPage.CustomEndpoint = customEndpointTextBox.Text;
        }

        private void organizationTextBox_TextChanged(object sender, EventArgs e)
        {
            optionsDialogPage.Organization = organizationTextBox.Text;
        }

        private void ignoreUnknownCACheckBox_CheckedChanged(object sender, EventArgs e)
        {
            optionsDialogPage.IgnoreUnknownCA = ignoreUnknownCACheckBox.Checked;
        }

        private void tokenTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs cancelEventArgs)
        {
            if (string.IsNullOrEmpty(tokenTextBox.Text))
            {
                errorProvider.SetError(tokenTextBox, "");

                return;
            }

            if (string.IsNullOrWhiteSpace(tokenTextBox.Text) || !IsValidGuid(tokenTextBox.Text))
            {
                cancelEventArgs.Cancel = true;

                tokenTextBox.Focus();

                errorProvider.SetError(tokenTextBox, "Not valid GUID.");
            }
            else
            {
                cancelEventArgs.Cancel = false;
                errorProvider.SetError(tokenTextBox, "");
            }
        }

        private void customEndpointTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs cancelEventArgs)
        {
            if (string.IsNullOrEmpty(tokenTextBox.Text))
            {
                errorProvider.SetError(tokenTextBox, "");

                return;
            }

            if (!string.IsNullOrWhiteSpace(customEndpointTextBox.Text) && !IsValidUrl(customEndpointTextBox.Text))
            {
                cancelEventArgs.Cancel = true;

                customEndpointTextBox.Focus();

                errorProvider.SetError(customEndpointTextBox, "Not valid URL.");
            }
            else
            {
                cancelEventArgs.Cancel = false;

                errorProvider.SetError(customEndpointTextBox, "");
            }
        }

        private void SnykGeneralSettingsUserControl_Load(object sender, EventArgs e)
        {
            InitializeApiToken();
        }

        private void usageAnalyticsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            optionsDialogPage.UsageAnalyticsEnabled = ignoreUnknownCACheckBox.Checked;

            optionsDialogPage.ServiceProvider.AnalyticsService.AnalyticsEnabled = usageAnalyticsCheckBox.Checked;

            optionsDialogPage.ServiceProvider.AnalyticsService.ObtainUser(optionsDialogPage.ServiceProvider.GetApiToken());
        }
    }  
}
