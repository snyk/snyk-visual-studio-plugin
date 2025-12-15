using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
        private HtmlSettingsPanel htmlSettingsPanel;
        private bool useHtmlPanel;
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
            // Only reset scrollbar for legacy control
            if (useHtmlPanel || this.generalSettingsUserControl == null)
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
            // Only handle authentication UI updates for legacy panel
            // HTML panel gets auth updates via Language Server
            if (!useHtmlPanel)
            {
                await this.GeneralSettingsUserControl.HandleAuthenticationSuccess(token, apiUrl);
            }
        }

        public async Task HandleFailedAuthentication(string errorMessage)
        {
            // Only handle authentication UI updates for legacy panel
            // HTML panel gets auth updates via Language Server
            if (!useHtmlPanel)
            {
                await this.GeneralSettingsUserControl.HandleFailedAuthentication(errorMessage);
            }
        }

        protected override IWin32Window Window
        {
            get
            {
                // Check feature flag to determine which panel to use
                useHtmlPanel = FeatureFlags.UseHtmlConfigDialog;

                if (useHtmlPanel)
                {
                    return HtmlPanel;
                }
                else
                {
                    return GeneralSettingsUserControl;
                }
            }
        }

        private HtmlSettingsPanel HtmlPanel
        {
            get
            {
                if (htmlSettingsPanel == null)
                {
                    var rpc = GetLanguageServerRpc();
                    htmlSettingsPanel = new HtmlSettingsPanel(SnykOptions, rpc);
                }
                return htmlSettingsPanel;
            }
        }

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

        private IJsonRpc GetLanguageServerRpc()
        {
            try
            {
                // Get RPC from Language Client Manager
                if (serviceProvider?.LanguageClientManager != null)
                {
                    return serviceProvider.LanguageClientManager.Rpc;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to get Language Server RPC, HTML panel will use fallback");
            }
            return null; // Fallback HTML will be used
        }

        // This method is used when the user clicks "Ok"
        public override void SaveSettingsToStorage()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                if (useHtmlPanel && htmlSettingsPanel != null)
                {
                    // HTML panel handles its own saving via JavaScript bridge
                    try
                    {
                        await htmlSettingsPanel.ApplyAsync();
                        Logger.Information("HTML settings panel saved successfully");

                        // Still need to save to storage and handle CLI changes
                        this.serviceProvider.SnykOptionsManager.Save(this.SnykOptions);

                        // Check for CLI changes (options may have been updated by HTML panel)
                        // Note: HTML panel updates ISnykOptions directly, so we can check for changes
                        // For now, restart LS to pick up any HTML-provided changes
                        if (LanguageClientHelper.IsLanguageServerReady())
                        {
                            await serviceProvider.LanguageClientManager.DidChangeConfigurationAsync(
                                SnykVSPackage.Instance.DisposalToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to save HTML settings panel");
                    }
                }
                else
                {
                    // Legacy panel - use existing save logic
                    HandleScanConfiguration();
                    HandleExperimentalConfiguration();
                    HandleUserExperienceConfiguration();
                    HandleGeneralConfiguration();
                    await HandleSolutionOptionsConfigurationAsync();
                    var hasCliChanges = HandleCliConfiguration();

                    this.serviceProvider.SnykOptionsManager.Save(this.SnykOptions);

                    if (hasCliChanges)
                    {
                        HandleCliChange();
                        return;
                    }
                }
            }).FireAndForget();
        }

        private async Task HandleSolutionOptionsConfigurationAsync()
        {
            var control = this.serviceProvider.Package.SnykSolutionOptionsDialogPage.SnykSolutionOptionsUserControl;

            // Save additional options
            if(control.AdditionalOptions != null)
                await this.serviceProvider.SnykOptionsManager.SaveAdditionalOptionsAsync(control.AdditionalOptions);

            // Implement logic for organization
            // Use UI state instead of reading from database to get current user intent
            var isAutoMode = control.IsAutoOrganizationChecked;
            var preferredOrganizationText = control.Organization;

            await this.serviceProvider.SnykOptionsManager.SaveOrgSetByUserAsync(!isAutoMode);
            await this.serviceProvider.SnykOptionsManager.SavePreferredOrgAsync(preferredOrganizationText);
            await this.UpdateFolderConfigForCurrentSolutionAsync();
        }

        private async Task UpdateFolderConfigForCurrentSolutionAsync()
        {
            string solutionPath = null;
            try
            {
                // Get current solution folder path
                solutionPath = await this.serviceProvider.SolutionService.GetSolutionFolderAsync();

                // Get current folder configs
                var folderConfigs = this.serviceProvider.Options.FolderConfigs ?? new List<FolderConfig>();

                // Find or create folder config for current solution
                var existingConfig = folderConfigs.FirstOrDefault(fc =>
                    string.Equals(fc.FolderPath, solutionPath, StringComparison.OrdinalIgnoreCase));

                if (existingConfig == null)
                {
                    // Create new folder config
                    existingConfig = new FolderConfig
                    {
                        FolderPath = solutionPath,
                        BaseBranch = "main", // Default branch
                        LocalBranches = new List<string> { "main" },
                        AdditionalParameters = new List<string>(),
                        OrgSetByUser = true,
                        OrgMigratedFromGlobalConfig = false
                    };
                    folderConfigs.Add(existingConfig);
                }

                // Read values from SnykOptionsManager (already saved by HandleSolutionOptionsConfigurationAsync)
                var orgSetByUser = await this.serviceProvider.SnykOptionsManager.GetOrgSetByUserAsync();
                var preferredOrg = await this.serviceProvider.SnykOptionsManager.GetPreferredOrgAsync();

                existingConfig.OrgSetByUser = orgSetByUser;
                existingConfig.PreferredOrg = preferredOrg ?? string.Empty;

                // Update global folder configs
                this.serviceProvider.Options.FolderConfigs = folderConfigs;

                Logger.Information("Updated folder config for solution: {SolutionPath}, OrgSetByUser: {OrgSetByUser}, PreferredOrg: {PreferredOrg}",
                    solutionPath, orgSetByUser, existingConfig.PreferredOrg);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to update folder config for solution: {SolutionPath}", solutionPath);
            }
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

        private void HandleGeneralConfiguration()
        {
            // Read organization from the general settings control
            // This is consistent with how other settings are handled - read on OK/Apply, not on every keystroke
            var control = this.GeneralSettingsUserControl;
            this.SnykOptions.Organization = control.Organization;
        }

        private void HandleScanConfiguration()
        {
            var memento = this.serviceProvider.Package.SnykScanOptionsDialogPage.SnykScanOptionsUserControl.OptionsMemento;

            this.SnykOptions.EnableDeltaFindings = memento.EnableDeltaFindings;
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
