using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using Serilog;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Extension;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.UI.Notifications;
using Task = System.Threading.Tasks.Task;
using Timer = System.Windows.Forms.Timer;

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

        private static readonly int TwoSecondsDelay = 2000;

        private const int MaxSastRequestAttempts = 20;

        private readonly Timer snykCodeEnableTimer = new Timer();

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykGeneralSettingsUserControl"/> class.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public SnykGeneralSettingsUserControl(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            snykOptions = this.serviceProvider.Options;
            this.InitializeComponent();
            this.Initialize();
        }
        
        /// <summary>
        /// Initialize elements and actions.
        /// </summary>
        private void Initialize()
        {
            Logger.Information("Enter Initialize method");
            
            this.UpdateViewFromOptions();
            snykOptions.SettingsChanged += this.OptionsDialogPageOnSettingsChanged;
            this.Load += this.SnykGeneralSettingsUserControl_Load;

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
            this.organizationTextBox.Text = snykOptions.Organization;
            this.ignoreUnknownCACheckBox.Checked = snykOptions.IgnoreUnknownCA;
            this.ossEnabledCheckBox.Checked = snykOptions.OssEnabled;
            this.iacEnabledCheckbox.Checked = snykOptions.IacEnabled;
            this.autoScanCheckBox.Checked = snykOptions.AutoScan;
            this.tokenTextBox.Text = snykOptions.ApiToken.ToString();
            this.cbIgnoredIssues.Checked = snykOptions.IgnoredIssuesEnabled;
            this.cbOpenIssues.Checked = snykOptions.OpenIssuesEnabled;

            if (cbDelta.DataSource == null)
            {
                this.cbDelta.DataSource = DeltaOptionList();
            }

            if (authType.SelectedIndex == -1)
            {
                this.authType.DataSource = AuthenticationMethodList();
                this.authType.DisplayMember = "Description";
                this.authType.ValueMember = "Value";
            }

            this.authType.SelectedValue = snykOptions.AuthenticationMethod;
            this.cbDelta.SelectedItem = snykOptions.EnableDeltaFindings ? "Net new issues" : "All issues";
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

        private IEnumerable<string> DeltaOptionList()
        {
            var defaultList = new List<string> { "All issues", "Net new issues"};
            return defaultList;
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

        public void InvalidateApiToken()
        {
            this.tokenTextBox.Text = string.Empty;
            if(LanguageClientHelper.IsLanguageServerReady())
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await serviceProvider.LanguageClientManager.InvokeLogout(SnykVSPackage.Instance.DisposalToken);
                }).FireAndForget();
        }

        public async Task HandleFailedAuthentication(string errorMessage)
        {
            Logger.Information("Enter authenticate errorCallback");

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            AuthDialogWindow.Instance.Hide();

            var ossError = new OssError
            {
                IsSuccess = false,
                Message = errorMessage,
                Path = string.Empty,
            };

            this.serviceProvider.TasksService.FireOssError(ossError);

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
            if (this.ValidateChildren(ValidationConstraints.Enabled))
            {
                snykOptions.ApiToken = new AuthenticationToken(snykOptions.AuthenticationMethod, this.tokenTextBox.Text); 
                serviceProvider.Options.InvokeSettingsChangedEvent();
            }
        }

        private void CustomEndpointTextBox_LostFocus(object sender, EventArgs e)
        {
            if (this.ValidateChildren(ValidationConstraints.Enabled))
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
        }

        private void IgnoreUnknownCACheckBox_CheckedChanged(object sender, EventArgs e)
        {
            snykOptions.IgnoreUnknownCA = this.ignoreUnknownCACheckBox.Checked;
            serviceProvider.Options.InvokeSettingsChangedEvent();
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

        private void SnykGeneralSettingsUserControl_Load(object sender, EventArgs e)
        {
            this.StartSastEnablementCheckLoop();
            this.CheckForIgnores();
        }

        private void UpdateSnykCodeEnablementSettings(SastSettings sastSettings)
        {
            var snykCodeEnabled = sastSettings?.SnykCodeEnabled ?? false;

            if (!snykCodeEnabled)
            {
                this.snykCodeDisabledInfoLabel.Text = "Snyk Code is disabled by your organisation\'s configuration:";
            }

            this.codeSecurityEnabledCheckBox.Enabled = snykCodeEnabled;
            this.codeQualityEnabledCheckBox.Enabled = snykCodeEnabled;
            this.snykCodeDisabledInfoLabel.Visible = !snykCodeEnabled;
            this.snykCodeSettingsLinkLabel.Visible = !snykCodeEnabled;
            this.checkAgainLinkLabel.Visible = !snykCodeEnabled;
        }

        private void CheckForIgnores()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                if (!snykOptions.ConsistentIgnoresEnabled && LanguageClientHelper.IsLanguageServerReady())
                    await serviceProvider.FeatureFlagService.RefreshAsync(SnykVSPackage.Instance.DisposalToken);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.ignoreGroupbox.Visible = snykOptions.ConsistentIgnoresEnabled;
            }).FireAndForget();
        }
        private void StartSastEnablementCheckLoop()
        {
            try
            {
                if (this.snykCodeEnableTimer.Enabled)
                {
                    this.snykCodeEnableTimer.Stop();
                }

                var currentRequestAttempt = 1;

                this.snykCodeEnableTimer.Interval = TwoSecondsDelay;

                this.snykCodeEnableTimer.Tick += (sender, args) => ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        if (!LanguageClientHelper.IsLanguageServerReady()) return;
                        var sastSettings = await this.serviceProvider.LanguageClientManager.InvokeGetSastEnabled(SnykVSPackage.Instance.DisposalToken);

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

        private void OssEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            snykOptions.OssEnabled = this.ossEnabledCheckBox.Checked;
        }

        private void CodeSecurityEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            snykOptions.SnykCodeSecurityEnabled = this.codeSecurityEnabledCheckBox.Checked;
        }

        private void CodeQualityEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            snykOptions.SnykCodeQualityEnabled = this.codeQualityEnabledCheckBox.Checked;
        }

        private void SnykCodeSettingsLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            => Process.Start(snykOptions.SnykCodeSettingsUrl);

        private void CheckAgainLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.StartSastEnablementCheckLoop();
        }

        private void OrganizationInfoLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.OrganizationInfoLink.LinkVisited = true;
            Process.Start("https://docs.snyk.io/ide-tools/visual-studio-extension#organization-setting");
        }

        private void authType_SelectionChangeCommitted(object sender, EventArgs e)
        {
            snykOptions.AuthenticationMethod = (AuthenticationType)authType.SelectedValue;
            serviceProvider.Options.InvokeSettingsChangedEvent();
            InvalidateApiToken();
        }

        private void autoScanCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            snykOptions.AutoScan = autoScanCheckBox.Checked;
        }

        private void iacEnabledCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            snykOptions.IacEnabled = iacEnabledCheckbox.Checked;
        }


        private void cbOpenIssues_CheckedChanged(object sender, EventArgs e)
        {
            snykOptions.OpenIssuesEnabled = this.cbOpenIssues.Checked;
        }

        private void cbIgnoredIssues_CheckedChanged(object sender, EventArgs e)
        {
            snykOptions.IgnoredIssuesEnabled = this.cbIgnoredIssues.Checked;
        }
        private void cbDelta_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (this.cbDelta.SelectedItem == null)
                return;
            var enableDelta = this.cbDelta.SelectedItem.ToString() == "Net new issues";
            snykOptions.EnableDeltaFindings = enableDelta;
        }

        private void organizationTextBox_TextChanged(object sender, EventArgs e)
        {
            snykOptions.Organization = organizationTextBox.Text;
        }

        public Panel GetPanel()
        {
            return this.mainPanel;
        }

    }
}
