using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Serilog;
using Snyk.VisualStudio.Extension;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Extension;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Task = System.Threading.Tasks.Task;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Control for Snyk General Settings.
    /// </summary>
    public partial class SnykGeneralSettingsUserControl : UserControl
    {
        private readonly ISnykServiceProvider serviceProvider;
        private static readonly ILogger Logger = LogManager.ForContext<SnykGeneralSettingsUserControl>();
        /// <summary>
        /// Instance of SnykGeneralOptionsDialogPage.
        /// </summary>
        private readonly ISnykOptions snykOptions;
        private readonly System.ComponentModel.ComponentResourceManager resources;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykGeneralSettingsUserControl"/> class.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public SnykGeneralSettingsUserControl(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            snykOptions = this.serviceProvider.Options;
            this.resources = new System.ComponentModel.ComponentResourceManager(typeof(SnykGeneralSettingsUserControl));
            this.Load += OnLoad;
            this.InitializeComponent();
            this.Initialize();
        }

        /// <summary>
        /// Gets the organization value from the organization text box.
        /// </summary>
        public string Organization => this.organizationTextBox.Text;

        private void OnLoad(object sender, EventArgs e)
        {
            CheckForIgnores();
        }

        private void CheckForIgnores()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                if (serviceProvider.Options.ApiToken.IsValid() && !serviceProvider.Options.ConsistentIgnoresEnabled && LanguageClientHelper.IsLanguageServerReady())
                    await serviceProvider.FeatureFlagService.RefreshAsync(SnykVSPackage.Instance.DisposalToken);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }).FireAndForget();
        }

        /// <summary>
        /// Initialize elements and actions.
        /// </summary>
        private void Initialize()
        {
            Logger.Information("Enter Initialize method");
            
            this.UpdateViewFromOptions();
            snykOptions.SettingsChanged += this.OptionsDialogPageOnSettingsChanged;

            if (LanguageClientHelper.LanguageClientManager() != null)
            {
                LanguageClientHelper.LanguageClientManager().OnLanguageClientNotInitializedAsync += OnOnLanguageClientNotInitializedAsync;
                LanguageClientHelper.LanguageClientManager().OnLanguageServerReadyAsync += OnOnLanguageServerReadyAsync;
            }
            this.serviceProvider.ToolWindow.Show();
            Logger.Information("Leave Initialize method");
        }

        private async Task OnOnLanguageServerReadyAsync(object sender, SnykLanguageServerEventArgs args)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            authenticateButton.Enabled = true;
        }

        private async Task OnOnLanguageClientNotInitializedAsync(object sender, SnykLanguageServerEventArgs args)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            authenticateButton.Enabled = false;
        }

        private void UpdateViewFromOptions()
        {
            this.authenticateButton.Enabled = LanguageClientHelper.IsLanguageServerReady();
            this.customEndpointTextBox.Text = snykOptions.CustomEndpoint;
            this.organizationTextBox.Text = snykOptions.Organization ?? string.Empty;
            this.ignoreUnknownCACheckBox.Checked = snykOptions.IgnoreUnknownCA;
            this.tokenTextBox.Text = snykOptions.ApiToken.ToString();

            if (authType.SelectedIndex == -1)
            {
                this.authType.DataSource = AuthenticationMethodList();
                this.authType.DisplayMember = "Description";
                this.authType.ValueMember = "Value";
            }

            this.authType.SelectedValue = snykOptions.AuthenticationMethod;
        }

        
        private IEnumerable<object> AuthenticationMethodList()
        {
            return Enum.GetValues(typeof(AuthenticationType))
                .Cast<Enum>()
                .Select(value => new
                {
                    (Attribute.GetCustomAttribute(value.GetType().GetField(value.ToString()), typeof(DescriptionAttribute)) as DescriptionAttribute)?.Description,
                    Value = value
                })
                .ToList();
        }

        private void OptionsDialogPageOnSettingsChanged(object sender, SnykSettingsChangedEventArgs e) =>
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.UpdateViewFromOptions();
            }).FireAndForget();

        public async Task HandleAuthenticationSuccess(string apiToken, string apiUrl)
        {
            Logger.Information("Enter authenticate successCallback");

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            AuthDialogWindow.Instance.Hide();
            if (this.tokenTextBox.IsHandleCreated)
            {
                this.tokenTextBox.Invoke(new Action(() =>
                {
                    this.tokenTextBox.Text = apiToken;
                }));
            }

            if (this.customEndpointTextBox.IsHandleCreated)
            {
                this.tokenTextBox.Invoke(new Action(() =>
                {
                    this.customEndpointTextBox.Text = apiUrl;
                }));
            }

            if (this.authenticateButton.IsHandleCreated)
            {
                this.authenticateButton.Invoke(new Action(() =>
                {
                    this.errorProvider.SetError(this.tokenTextBox, string.Empty);
                }));
            }

            await this.serviceProvider.ToolWindow.UpdateScreenStateAsync();
        }

        public async Task HandleFailedAuthentication(string errorMessage)
        {
            Logger.Information("Enter authenticate errorCallback");

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            AuthDialogWindow.Instance.Hide();

            var presentableError = new Language.PresentableError
            {
                ErrorMessage = errorMessage,
                Path = string.Empty,
                ShowNotification = true
            };

            this.serviceProvider.TasksService.FireOssError(presentableError);

            this.serviceProvider.ToolWindow.Show();

            await this.serviceProvider.ToolWindow.UpdateScreenStateAsync();
        }

        private void AuthenticateButton_Click(object sender, EventArgs eventArgs) => ThreadHelper.JoinableTaskFactory
            .RunAsync(this.AuthenticateButtonClickAsync);

        private async Task AuthenticateButtonClickAsync()
        {
            Logger.Information("Enter authenticateButton_Click method");
            Logger.Information("Start run task");
            await TaskScheduler.Default;


            if (SnykCli.IsCliFileFound(serviceProvider.Options.CliCustomPath))
            {
                serviceProvider.GeneralOptionsDialogPage.Authenticate();
            }
            else
            {
                Logger.Information("CLI doesn't exists. Download CLI before get Api token");
                await serviceProvider.TasksService.DownloadAsync(serviceProvider.GeneralOptionsDialogPage.Authenticate);
            }
        }

        private bool IsValidUrl(string url) => Uri.IsWellFormedUriString(url, UriKind.Absolute);

        private void TokenTextBox_TextChanged(object sender, EventArgs e)
        {
            snykOptions.ApiToken = new AuthenticationToken(snykOptions.AuthenticationMethod, this.tokenTextBox.Text); 
        }

        private void CustomEndpointTextBox_LostFocus(object sender, EventArgs e)
        {
            if (!Uri.IsWellFormedUriString(this.customEndpointTextBox.Text, UriKind.Absolute))
            {
                Logger.Warning("Custom endpoint value is not a well-formed URI. Setting custom endpoint to empty string");
                this.customEndpointTextBox.Text = snykOptions.CustomEndpoint = string.Empty;
                return;
            }

            snykOptions.CustomEndpoint = ApiEndpointResolver.TranslateOldApiToNewApiEndpoint(this.customEndpointTextBox.Text);
            serviceProvider.Options.InvokeSettingsChangedEvent();
        }

        private void IgnoreUnknownCACheckBox_CheckedChanged(object sender, EventArgs e)
        {
            snykOptions.IgnoreUnknownCA = this.ignoreUnknownCACheckBox.Checked;
        }


        private void TokenTextBox_Validating(object sender, CancelEventArgs cancelEventArgs) =>
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await this.serviceProvider.ToolWindow.UpdateScreenStateAsync();

                if (string.IsNullOrEmpty(this.tokenTextBox.Text))
                {
                    this.errorProvider.SetError(this.tokenTextBox, string.Empty);

                    return;
                }

                if (this.tokenTextBox.Text.IsNullOrEmpty())
                {
                    cancelEventArgs.Cancel = true;

                    this.tokenTextBox.Focus();

                    this.errorProvider.SetError(this.tokenTextBox, "Invalid Token");
                }
                else
                {
                    cancelEventArgs.Cancel = false;
                    this.errorProvider.SetError(this.tokenTextBox, string.Empty);
                }
            });

        private void CustomEndpointTextBox_Validating(object sender, CancelEventArgs cancelEventArgs)
        {
            if (string.IsNullOrWhiteSpace(this.customEndpointTextBox.Text) || this.IsValidUrl(this.customEndpointTextBox.Text))
            {
                this.errorProvider.SetNoError(this.customEndpointTextBox);
                return;
            }

            cancelEventArgs.Cancel = true;
            this.errorProvider.SetError(this.customEndpointTextBox, "Needs to be a full absolute well-formed URL (including protocol)");
        }
       

        private void authType_SelectionChangeCommitted(object sender, EventArgs e)
        {
            snykOptions.AuthenticationMethod = (AuthenticationType)authType.SelectedValue;
            if(LanguageClientHelper.IsLanguageServerReady())
                LanguageClientHelper.LanguageClientManager().DidChangeConfigurationAsync(SnykVSPackage.Instance.DisposalToken).FireAndForget();
        }


        public Panel GetPanel()
        {
            return this.mainPanel;
        }

        private void SnykRegionsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.SnykRegionsLink.LinkVisited = true;
            Process.Start("https://docs.snyk.io/working-with-snyk/regional-hosting-and-data-residency#available-snyk-regions");
        }

        private void organizationDescriptionText_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
