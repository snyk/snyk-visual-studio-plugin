using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Snyk general settings page.
    /// </summary>
    [Guid("d45468c1-33d2-4dca-9780-68abaedf95e7")]
    [ComVisible(true)]
    public class SnykGeneralOptionsDialogPage : DialogPage, ISnykGeneralOptionsDialogPage
    {
        private ISnykServiceProvider serviceProvider;
        private SnykGeneralSettingsUserControl generalSettingsUserControl;
        private static readonly ILogger Logger = LogManager.ForContext<SnykGeneralOptionsDialogPage>();

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);

            ResetScrollbarSettings();
        }

        protected override void OnClosed(EventArgs e)
        {
            ResetScrollbarSettings();
        }

        private void ResetScrollbarSettings()
        {
            if (this.generalSettingsUserControl == null)
                return;

            // Reset the scroll position
            this.generalSettingsUserControl.GetPanel().AutoScrollPosition = new System.Drawing.Point(0, 0);
            this.generalSettingsUserControl.GetPanel().Top = 0;
            this.generalSettingsUserControl.Top = 0;
            // Force a refresh if needed
            this.generalSettingsUserControl.Invalidate(true);
            this.generalSettingsUserControl.Update();
        }

        public void Initialize(ISnykServiceProvider provider)
        {
            this.serviceProvider = provider;
            this.SnykOptions = provider.Options;
            SnykOptions.SettingsChanged += SnykGeneralOptionsDialogPage_SettingsChanged;
        }

        public ISnykOptions SnykOptions { get; set; }

        public async Task HandleAuthenticationSuccess(string token, string apiUrl)
        {
            await this.GeneralSettingsUserControl.HandleAuthenticationSuccess(token, apiUrl);
        }

        public async Task HandleFailedAuthentication(string errorMessage)
        {
            await this.GeneralSettingsUserControl.HandleFailedAuthentication(errorMessage);
        }

        protected override IWin32Window Window => GeneralSettingsUserControl;

        public SnykGeneralSettingsUserControl GeneralSettingsUserControl
        {
            get
            {
                if (generalSettingsUserControl == null)
                {
                    generalSettingsUserControl = new SnykGeneralSettingsUserControl(serviceProvider);
                }
                return generalSettingsUserControl;
            }
        }

        // This method is used when the user clicks "Ok"
        public override void SaveSettingsToStorage()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(() =>
            {
                HandleScanConfiguration();
                HandleExperimentalConfiguration();
                HandleUserExperienceConfiguration();
                HandleSolutionOptionsConfiguration();

                var hasCliChanges = HandleCliConfiguration();

                this.serviceProvider.SnykOptionsManager.Save(this.SnykOptions);

                if (hasCliChanges)
                {
                    HandleCliChange();
                    return Task.CompletedTask;
                }

                if (LanguageClientHelper.IsLanguageServerReady() && this.SnykOptions.AutoScan)
                    this.serviceProvider.TasksService.ScanAsync().FireAndForget();

                return Task.CompletedTask;
            }).FireAndForget();
        }

        private void HandleSolutionOptionsConfiguration()
        {
            var memento = this.serviceProvider.Package.SnykSolutionOptionsDialogPage.SnykSolutionOptionsUserControl.AdditionalOptions;
            if(memento != null)
                this.serviceProvider.SnykOptionsManager.SaveAdditionalOptionsAsync(memento).FireAndForget();
        }

        private void HandleUserExperienceConfiguration()
        {
            var memento = this.serviceProvider.Package.SnykUserExperienceDialogPage.SnykUserExperienceUserControl.OptionsMemento;

            this.SnykOptions.AutoScan = memento.AutoScan;
        }

        private void HandleExperimentalConfiguration()
        {
            var memento = this.serviceProvider.Package.SnykExperimentalDialogPage.SnykExperimentalUserControl.OptionsMemento;

            this.SnykOptions.IgnoredIssuesEnabled = memento.IgnoredIssuesEnabled;
            this.SnykOptions.OpenIssuesEnabled = memento.OpenIssuesEnabled;
        }

        private void HandleScanConfiguration()
        {
            var memento = this.serviceProvider.Package.SnykScanOptionsDialogPage.SnykScanOptionsUserControl.OptionsMemento;

            this.SnykOptions.EnableDeltaFindings = memento.EnableDeltaFindings;
            this.SnykOptions.SnykCodeQualityEnabled = memento.SnykCodeQualityEnabled;
            this.SnykOptions.SnykCodeSecurityEnabled = memento.SnykCodeSecurityEnabled;
            this.SnykOptions.IacEnabled = memento.IacEnabled;
            this.SnykOptions.OssEnabled = memento.OssEnabled;
        }

        private bool HandleCliConfiguration()
        {
            var memento = this.serviceProvider.Package.SnykCliOptionsDialogPage.SnykCliOptionsUserControl.OptionsMemento;

            this.SnykOptions.CliDownloadUrl = memento.CliDownloadUrl;

            var binariesAutoUpdateChanged = this.SnykOptions.BinariesAutoUpdate != memento.BinariesAutoUpdate;
            var releaseChannelChanged = this.SnykOptions.CliReleaseChannel != memento.CliReleaseChannel;
            var cliCustomPathChanged = this.SnykOptions.CliCustomPath != memento.CliCustomPath;

            this.SnykOptions.CurrentCliVersion = memento.CurrentCliVersion;
            this.SnykOptions.BinariesAutoUpdate = memento.BinariesAutoUpdate;
            this.SnykOptions.CliReleaseChannel = memento.CliReleaseChannel;
            this.SnykOptions.CliCustomPath = memento.CliCustomPath;

            var hasChanges = binariesAutoUpdateChanged || releaseChannelChanged || cliCustomPathChanged;
            return hasChanges;
        }

        private void HandleCliChange()
        {
            serviceProvider.TasksService.CancelTasks();

            if (this.SnykOptions.BinariesAutoUpdate)
            {
                // DownloadStarted event stops language server and DownloadFinished starts it automatically
                this.serviceProvider.TasksService.Download();
            }
            else
            {
                LanguageClientHelper.LanguageClientManager().RestartServerAsync().FireAndForget();
            }
        }

        private void SnykGeneralOptionsDialogPage_SettingsChanged(object sender, SnykSettingsChangedEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                if (LanguageClientHelper.IsLanguageServerReady())
                {
                    await serviceProvider.LanguageClientManager.DidChangeConfigurationAsync(SnykVSPackage
                        .Instance.DisposalToken);
                }
            }).FireAndForget();
        }

        public void Authenticate()
        {
            Logger.Information("Enter Authenticate method");
            if (!SnykCli.IsCliFileFound(this.SnykOptions.CliCustomPath))
            {
                throw new FileNotFoundException("CLI not found");
            }
            try
            {
                if (!LanguageClientHelper.IsLanguageServerReady())
                {
                    Logger.Error("Language Server is not initialized yet.");
                    return;
                }

                if (this.SnykOptions.ApiToken.IsValid())
                {
                    if (SnykOptions.AutoScan)
                    {
                        ThreadHelper.JoinableTaskFactory.RunAsync(serviceProvider.TasksService.ScanAsync).FireAndForget();
                    }
                    return;
                }

                Logger.Information("Api token is invalid. Attempting to authenticate via snyk auth");

                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await serviceProvider.LanguageClientManager.InvokeLogout(SnykVSPackage.Instance
                        .DisposalToken);
                    Logger.Information("Invoking InvokeLogin for auth");
                    await serviceProvider.LanguageClientManager.InvokeLogin(SnykVSPackage.Instance
                        .DisposalToken);
                }).FireAndForget();

                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    // don't show auth dialog for PAT since we won't wait for response.
                    if (this.SnykOptions.AuthenticationMethod == AuthenticationType.Pat)
                        return;
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    Logger.Information("Attempting to call AuthDialogWindow.Instance.ShowDialog()");
                    AuthDialogWindow.Instance.ShowDialog();
                });
            }
            catch (Exception e)
            {
                Logger.Error(e, "Couldn't execute Invoke Login through LS.");
            }
        }

    }
}
