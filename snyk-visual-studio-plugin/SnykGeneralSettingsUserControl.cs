using System;
using System.Windows.Forms;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.CLI;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.UI
{   
    public partial class SnykGeneralSettingsUserControl : UserControl
    {
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

        private void GetApiToken()
        {
            var package = optionsDialogPage.Package;

            var cli = new SnykCli
            {
                Options = optionsDialogPage
            };

            string apiToken = cli.GetApiToken();

            if (String.IsNullOrEmpty(apiToken))
            {
                cli.Authenticate();

                apiToken = cli.GetApiToken();
            }

            tokenTextBox.Invoke((MethodInvoker)delegate
            {
                tokenTextBox.Text = apiToken;
            });            
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
