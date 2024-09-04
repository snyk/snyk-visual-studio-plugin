using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Serilog;
using Snyk.Common;
using Snyk.Common.Authentication;
using Snyk.Common.Settings;
using Snyk.VisualStudio.Extension.CLI;
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
        private static readonly ILogger logger = LogManager.ForContext<SnykGeneralSettingsUserControl>();

        /// <summary>
        /// Instance of SnykGeneralOptionsDialogPage.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed.")]
        internal SnykGeneralOptionsDialogPage OptionsDialogPage;

        private static readonly int TwoSecondsDelay = 2000;

        private static readonly int MaxSastRequestAttempts = 20;

        private Timer snykCodeEnableTimer = new Timer();

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykGeneralSettingsUserControl"/> class.
        /// </summary>
        /// <param name="apiService">Snyk API service instance.</param>
        public SnykGeneralSettingsUserControl()
        {
            this.InitializeComponent();
        }

        private ISnykServiceProvider ServiceProvider => this.OptionsDialogPage.ServiceProvider;

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
            this.errorReportsCheckBox.Checked = this.OptionsDialogPage.ErrorReportsEnabled;
            this.ossEnabledCheckBox.Checked = this.OptionsDialogPage.OssEnabled;
            this.ManageBinariesAutomaticallyCheckbox.Checked = this.OptionsDialogPage.BinariesAutoUpdate;

            var cliPath = string.IsNullOrEmpty(this.OptionsDialogPage.CliCustomPath)
                ? SnykCli.GetSnykCliDefaultPath()
                : this.OptionsDialogPage.CliCustomPath;

            this.CliPathTextBox.Text = cliPath;

            if (authType.SelectedIndex == -1)
            {
                this.authType.DataSource = AuthenticationMethodList();
                this.authType.DisplayMember = "Description";
                this.authType.ValueMember = "Value";
            }
            this.authType.SelectedValue = this.OptionsDialogPage.AuthenticationMethod;
        }

        private static IEnumerable<object> AuthenticationMethodList()
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
                this.UpdateViewFromOptionsDialog();
                this.InitializeApiToken();
                await ServiceProvider.Package.LanguageClientManager.DidChangeConfigurationAsync(CancellationToken.None);
            }).FireAndForget();

        public async Task OnAuthenticationSuccessfulAsync(string apiToken)
        {
            logger.Information("Enter authenticate successCallback");

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
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

            await this.ServiceProvider.ToolWindow.UpdateScreenStateAsync();
        }

        private void InitializeApiToken()
        {
            this.tokenTextBox.Text = this.OptionsDialogPage.ApiToken.ToString();
        }

        public async Task OnAuthenticationFailAsync(string errorMessage)
        {
            logger.Information("Enter authenticate errorCallback");

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
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

            OssError ossError = new OssError
            {
                IsSuccess = false,
                Message = errorMessage,
                Path = string.Empty,
            };

            this.ServiceProvider.TasksService.FireOssError(ossError);

            this.ServiceProvider.ToolWindow.Show();

            await this.ServiceProvider.ToolWindow.UpdateScreenStateAsync();
        }

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

            var serviceProvider = this.ServiceProvider;

            if (IsCliFileFound(serviceProvider.Options.CliCustomPath))
            {
                var authenticated = serviceProvider.Options.Authenticate();
                if (authenticated)
                {
                    await OnAuthenticationSuccessfulAsync(serviceProvider.Options.ApiToken.ToString());
                }
                else
                {
                    await OnAuthenticationFailAsync("Authentication failed");
                }
            }
            else
            {
                logger.Information("CLI doesn't exists. Download CLI before get Api token");
                await serviceProvider.TasksService.DownloadAsync(() => this.OptionsDialogPage.Authenticate());
            }
        }

        public bool IsCliFileFound(string cliCustomPath)
        {
            var path = string.IsNullOrEmpty(cliCustomPath) ? SnykCli.GetSnykCliDefaultPath() : cliCustomPath;
            return File.Exists(path);
        }

        private bool IsValidUrl(string url) => Uri.IsWellFormedUriString(url, UriKind.Absolute);

        private void TokenTextBox_TextChanged(object sender, EventArgs e)
        {
            if (this.ValidateChildren(ValidationConstraints.Enabled))
            {
                this.OptionsDialogPage.SetApiToken(this.tokenTextBox.Text);
            }
        }

        private void CustomEndpointTextBox_LostFocus(object sender, EventArgs e)
        {
            if (this.ValidateChildren(ValidationConstraints.Enabled))
            {
                this.OptionsDialogPage.CustomEndpoint = this.customEndpointTextBox.Text;
            }
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
                await this.ServiceProvider.ToolWindow.UpdateScreenStateAsync();

                if (string.IsNullOrEmpty(this.tokenTextBox.Text))
                {
                    this.errorProvider.SetError(this.tokenTextBox, string.Empty);

                    return;
                }

                if (this.tokenTextBox.Text.IsNullOrEmpty())
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
            this.InitializeApiToken();

            this.StartSastEnablementCheckLoop();
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
                        if (this.ServiceProvider?.Package?.LanguageClientManager == null || this.ServiceProvider.Package.LanguageClientManager.IsReady == false) return;
                        var sastSettings = await this.ServiceProvider.Package.LanguageClientManager.InvokeGetSastEnabled(CancellationToken.None);

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

        private void errorReportsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.OptionsDialogPage.ErrorReportsEnabled = this.errorReportsCheckBox.Checked;
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
            this.StartSastEnablementCheckLoop();
        }

        private void OrganizationInfoLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.OrganizationInfoLink.LinkVisited = true;
            Process.Start("https://docs.snyk.io/ide-tools/visual-studio-extension#organization-setting");
        }

        private void ManageBinariesAutomaticallyCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            this.OptionsDialogPage.BinariesAutoUpdate = this.ManageBinariesAutomaticallyCheckbox.Checked;
        }

        private void CliPathBrowseButton_Click(object sender, EventArgs e)
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
            this.CliPathTextBox.Text = string.IsNullOrEmpty(this.OptionsDialogPage.CliCustomPath)
                ? SnykCli.GetSnykCliDefaultPath()
                : selectedCliPath;
        }

        private void ClearCliCustomPathButton_Click(object sender, EventArgs e)
        {
            this.SetCliCustomPathValue(string.Empty);
        }

        private void authType_SelectionChangeCommitted(object sender, EventArgs e)
        {
            this.OptionsDialogPage.AuthenticationMethod = (AuthenticationType)authType.SelectedValue;
        }

        private void autoScanCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.OptionsDialogPage.AutoScan = autoScanCheckBox.Checked;
        }
    }
}
