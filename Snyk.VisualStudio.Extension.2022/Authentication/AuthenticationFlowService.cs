// ABOUTME: Orchestrates IDE-side auth flow (login/logout, modal auth dialog).
// ABOUTME: Extracted from SnykGeneralOptionsDialogPage when that DialogPage was retired.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.VisualStudio.Extension.CLI;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Authentication
{
    public class AuthenticationFlowService : IAuthenticationFlowService
    {
        private static readonly ILogger Logger = LogManager.ForContext<AuthenticationFlowService>();

        private readonly ISnykServiceProvider serviceProvider;

        public AuthenticationFlowService(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void Authenticate()
        {
            Logger.Information("Enter Authenticate method");

            var options = serviceProvider.Options;
            if (!SnykCli.IsCliFileFound(options.CliCustomPath))
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

                if (options.ApiToken.IsValid())
                {
                    if (options.AutoScan)
                    {
                        ThreadHelper.JoinableTaskFactory
                            .RunAsync(serviceProvider.TasksService.ScanAsync)
                            .FireAndForget();
                    }
                    return;
                }

                Logger.Information("Api token is invalid. Attempting to authenticate via snyk auth");

                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await serviceProvider.LanguageClientManager.InvokeLogout(SnykVSPackage.Instance.DisposalToken);
                    Logger.Information("Invoking InvokeLogin for auth");
                    await serviceProvider.LanguageClientManager.InvokeLogin(SnykVSPackage.Instance.DisposalToken);
                }).FireAndForget();

                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    // PAT auth doesn't roundtrip through the modal dialog — no point showing it.
                    if (options.AuthenticationMethod == AuthenticationType.Pat)
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

        public async Task HandleAuthenticationSuccessAsync(string token, string apiUrl)
        {
            Logger.Information("Enter authenticate successCallback");

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            AuthDialogWindow.Instance.Hide();

            // The visible HTML settings form (if open) is updated by
            // SnykLanguageClientCustomTarget.OnHasAuthenticated calling
            // HtmlSettingsControl.Instance?.UpdateAuthToken; no IDE-side form updates here.

            await serviceProvider.ToolWindow.UpdateScreenStateAsync();
        }

        public async Task HandleFailedAuthenticationAsync(string errorMessage)
        {
            Logger.Information("Enter authenticate errorCallback");

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            AuthDialogWindow.Instance.Hide();

            var presentableError = new PresentableError
            {
                ErrorMessage = errorMessage,
                Path = string.Empty,
                ShowNotification = true,
            };

            serviceProvider.TasksService.FireOssError(presentableError);
            serviceProvider.ToolWindow.Show();
            await serviceProvider.ToolWindow.UpdateScreenStateAsync();
        }
    }
}
