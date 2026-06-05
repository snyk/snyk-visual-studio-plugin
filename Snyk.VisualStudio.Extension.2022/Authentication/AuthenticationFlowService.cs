// ABOUTME: Orchestrates IDE-side auth flow (LS readiness check, login/logout, modal auth dialog).
// ABOUTME: Only used by the Tool Window Message Panel. HTML settings page calls the LS Auth method directly.

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

                // FireAndForget returns immediately, so the outer catch can't observe failures
                // from these awaited LS calls — handle them here or any JSON-RPC error,
                // ObjectDisposedException (client torn down) or cancellation goes unlogged.
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        await serviceProvider.LanguageClientManager.InvokeLogout(SnykVSPackage.Instance.DisposalToken);
                        Logger.Information("Invoking InvokeLogin for auth");
                        await serviceProvider.LanguageClientManager.InvokeLogin(SnykVSPackage.Instance.DisposalToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when the LS client / VS host is torn down mid-flight; not an error.
                        Logger.Debug("Login/logout invocation through the Language Server was canceled.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Login/logout invocation through the Language Server failed.");
                        // The modal AuthDialogWindow is shown below and is normally dismissed by the
                        // OnHasAuthenticated callback. That callback never arrives when the LS call
                        // fails, so dismiss the dialog and surface the failure here — otherwise the
                        // user is left staring at a dialog that can never complete.
                        await HandleFailedAuthenticationAsync("Authentication failed. Check the Snyk logs for details.");
                    }
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
                // Covers the synchronous setup above (readiness check, scan dispatch, modal auth
                // dialog). The actual LS login/logout calls are awaited inside the FireAndForget
                // lambda and log their own failures there.
                Logger.Error(e, "Couldn't start the authentication flow.");
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
