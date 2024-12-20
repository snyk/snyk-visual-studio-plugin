using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.UI.Notifications;

namespace Snyk.VisualStudio.Extension.Settings
{
    public partial class SnykScanOptionsUserControl : UserControl
    {
        private readonly ISnykServiceProvider serviceProvider;
        private static readonly ILogger Logger = LogManager.ForContext<SnykScanOptionsUserControl>();
        public ISnykOptions OptionsMemento { get; set; }

        private static readonly int TwoSecondsDelay = 2000;

        private const int MaxSastRequestAttempts = 20;

        private readonly Timer snykCodeEnableTimer = new Timer();

        public SnykScanOptionsUserControl(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            OptionsMemento = serviceProvider.SnykOptionsManager.Load();
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            Logger.Information("Enter Initialize method");

            this.UpdateViewFromOptions();
            OptionsMemento.SettingsChanged += this.OptionsDialogPageOnSettingsChanged;
            this.Load += this.SnykScanOptionsUserControl_Load;
            this.serviceProvider.ToolWindow.Show();
            Logger.Information("Leave Initialize method");
        }

        private void SnykScanOptionsUserControl_Load(object sender, EventArgs e)
        {
            this.StartSastEnablementCheckLoop();
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
                        if (!LanguageClientHelper.IsLanguageServerReady() || !serviceProvider.Options.ApiToken.IsValid()) return;
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

        private void UpdateViewFromOptions()
        {
            this.ossEnabledCheckBox.Checked = OptionsMemento.OssEnabled;
            this.iacEnabledCheckbox.Checked = OptionsMemento.IacEnabled;

            if (cbDelta.DataSource == null)
            {
                this.cbDelta.DataSource = DeltaOptionList();
            }
            this.cbDelta.SelectedItem = OptionsMemento.EnableDeltaFindings ? "Net new issues" : "All issues";
        }

        private IEnumerable<string> DeltaOptionList()
        {
            var defaultList = new List<string> { "All issues", "Net new issues" };
            return defaultList;
        }

        private void OptionsDialogPageOnSettingsChanged(object sender, SnykSettingsChangedEventArgs e) =>
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.UpdateViewFromOptions();
            }).FireAndForget();

        private void OssEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            OptionsMemento.OssEnabled = this.ossEnabledCheckBox.Checked;
        }

        private void iacEnabledCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            OptionsMemento.IacEnabled = iacEnabledCheckbox.Checked;
        }

        private void CodeSecurityEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            OptionsMemento.SnykCodeSecurityEnabled = this.codeSecurityEnabledCheckBox.Checked;
        }

        private void CodeQualityEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            OptionsMemento.SnykCodeQualityEnabled = this.codeQualityEnabledCheckBox.Checked;
        }

        private void cbDelta_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (this.cbDelta.SelectedItem == null)
                return;
            var enableDelta = this.cbDelta.SelectedItem.ToString() == "Net new issues";
            OptionsMemento.EnableDeltaFindings = enableDelta;
        }

        private void SnykCodeSettingsLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            => Process.Start(OptionsMemento.SnykCodeSettingsUrl);

        private void CheckAgainLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.StartSastEnablementCheckLoop();
        }

    }
}
