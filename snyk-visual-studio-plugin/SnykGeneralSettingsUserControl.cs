using System;
using System.Windows.Forms;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.CLI;
using System.Text.RegularExpressions;

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
                    cli.Authenticate();

                    apiToken = cli.GetApiToken();                    
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

        private bool IsValidGuid(string guid)
        {
            if (guid != null)
            {

                if (GuidRegex.IsMatch(guid))
                {
                    new Guid(guid);

                    return true;
                }
            }

            return false;
        }

        private void tokenTextBox_TextChanged(object sender, EventArgs e)
        {
            optionsDialogPage.ApiToken = tokenTextBox.Text;
        }

        private void customEndpointTextBox_TextChanged(object sender, EventArgs e)
        {
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
    }  
}
