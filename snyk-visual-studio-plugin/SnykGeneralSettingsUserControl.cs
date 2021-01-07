using System;
using System.Windows.Forms;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.CLI;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.UI
{   
    public partial class SnykGeneralSettingsUserControl : UserControl
    {
        private static Regex GuidRegex = new Regex(@"^(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}$", RegexOptions.Compiled);

        internal SnykGeneralOptionsDialogPage optionsDialogPage;

        public SnykGeneralSettingsUserControl()
        {
            InitializeComponent();
        }
        
        public void Initialize()
        {
            tokenTextBox.Text = optionsDialogPage.ApiToken;
            customEndpointTextBox.Text = optionsDialogPage.CustomEndpoint;
            organizationTextBox.Text = optionsDialogPage.Organization;
            ignoreUnknownCACheckBox.Checked = optionsDialogPage.IgnoreUnknownCA;
        }
        
        private void authenticateButton_Click(object sender, EventArgs eventArgs)            
        {            
            this.authProgressBar.Visible = true;
            this.tokenTextBox.Enabled = false;
            this.authenticateButton.Enabled = false;

            Task.Run(() =>
            {
                var package = optionsDialogPage.Package;
                var tasksService = package.TasksService;

                Action<string> successCallback = (apiToken) =>
                {
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
                };

                Action<string> errorCallback = (errorMessage) =>
                {
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

                    package.ShowToolWindow();
                    package.GetToolWindow().DisplayError(cliError);
                };

                if (SnykCli.IsCliExists())
                {
                    SetupApiToken(successCallback, errorCallback);
                }
                else
                {
                    tasksService.DownloadFinished += (obj, args) =>
                    {
                        SetupApiToken(successCallback, errorCallback);
                    };

                    package.TasksService.Download();
                }
            });            
        }                
        
        private void SetupApiToken(Action<string> successCallback, Action<string> errorCallback)
        {           
            var cli = new SnykCli
            {
                Options = optionsDialogPage
            };

            string apiToken = "";

            try
            {
                apiToken = cli.GetApiToken();

                if (String.IsNullOrEmpty(apiToken))
                {                    
                    string authResultMessage = cli.Authenticate();

                    if (authResultMessage.Contains("Your account has been authenticated. Snyk is now ready to be used."))
                    {
                        apiToken = cli.GetApiToken();
                    }
                    else
                    {
                        errorCallback(authResultMessage);

                        return;
                    }
                }

                if (!IsValidGuid(apiToken))
                {
                    errorCallback("Invalid GUID.");

                    return;
                }

                successCallback(apiToken);
            } catch (Exception exception)
            {
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

        private void tokenTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tokenTextBox.Text) || !IsValidGuid(tokenTextBox.Text))
            {
                e.Cancel = true;

                tokenTextBox.Focus();

                errorProvider.SetError(tokenTextBox, "Not valid GUID.");
            }
            else
            {
                e.Cancel = false;
                errorProvider.SetError(tokenTextBox, "");
            }
        }

        private void customEndpointTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs cancelEventArgs)
        {
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
    }  
}
