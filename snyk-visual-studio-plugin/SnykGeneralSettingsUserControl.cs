using System;
using System.Windows.Forms;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.CLI;
using System.Threading.Tasks;
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
            authProgressBar.Visible = true;

            Task.Run(() => GetApiToken()).ContinueWith(task => 
            {                
                this.authProgressBar.Invoke((MethodInvoker)delegate {
                    this.authProgressBar.Visible = false;
                });
            });            
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

        private void GetApiToken()
        {
            var package = optionsDialogPage.Package;

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
                    throw new Exception("Invalid GUID.");
                }

                tokenTextBox.Invoke((MethodInvoker)delegate
                {
                    tokenTextBox.Text = apiToken;
                });
            } catch (Exception exception)
            {

                CliError cliError = new CliError
                {
                    IsSuccess = false,
                    Message = exception.Message,
                    Path = ""
                };

                package.ShowToolWindow();
                package.GetToolWindow().DisplayError(cliError);
            }                                    
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
