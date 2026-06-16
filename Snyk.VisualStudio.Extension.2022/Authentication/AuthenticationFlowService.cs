// ABOUTME: Orchestrates IDE-side auth flow (LS readiness check, login/logout, modal auth dialog).
// ABOUTME: Only used by the Tool Window Message Panel. HTML settings page calls the LS Auth method directly.

using System;
using System.IO;
using System.Threading;
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
        private readonly IAuthDialog injectedAuthDialog;

        // Re-entrancy guard for Authenticate(): 0 = idle, 1 = in flight. Interlocked because the
        // method can be invoked off the UI thread, so a plain check-then-set bool would race.
        private int authInProgress;

        // Resolve the dialog lazily: production passes null and falls back to the WPF singleton at
        // first use (unchanged timing — constructing it eagerly here could run off the UI thread),
        // while tests inject a fake so the orchestration runs without a real window.
        private IAuthDialog AuthDialog => this.injectedAuthDialog ?? AuthDialogWindow.Instance;

        public AuthenticationFlowService(ISnykServiceProvider serviceProvider, IAuthDialog authDialog = null)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.injectedAuthDialog = authDialog;
        }

        public void Authenticate()
        {
            Logger.Information("Enter Authenticate method");

            // Drop re-entrant / concurrent calls (e.g. a double-click): a second flow would start a
            // second LS logout/login sequence and re-arm / re-show the dialog. The guard is released
            // in the finally — which, for the OAuth path, is after the modal dialog is dismissed
            // (ThreadHelper...Run below blocks until then), so a second click while the dialog is up
            // is correctly ignored.
            if (Interlocked.CompareExchange(ref this.authInProgress, 1, 0) != 0)
            {
                Logger.Information("Authentication already in progress; ignoring re-entrant call.");
                return;
            }

            try
            {
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

                    // Arm the dialog's show/hide guard before launching login below. A fast
                    // success/failure can call HideForAuthResult() before ShowDialogForAuth() runs;
                    // arming first lets the show be skipped in that case instead of getting stuck.
                    AuthDialog.ArmForShow();

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
                        Logger.Information("Attempting to call AuthDialog.ShowDialogForAuth()");
                        // Skips the show if the auth result already arrived (and hid the dialog) first.
                        AuthDialog.ShowDialogForAuth();
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
            finally
            {
                // Release the re-entrancy guard. For the OAuth path this runs after the modal dialog
                // is dismissed (the Run above blocks until then); for early-return / PAT paths it runs
                // promptly. Either way a stuck guard can't permanently block future auth attempts.
                Interlocked.Exchange(ref this.authInProgress, 0);
            }
        }

        public async Task HandleAuthenticationSuccessAsync(string token, string apiUrl)
        {
            Logger.Information("Enter authenticate successCallback");

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            // HideForAuthResult (not Hide) so that if this success arrives before the modal show
            // has run, the pending show is suppressed rather than left stuck on screen.
            AuthDialog.HideForAuthResult();

            // The visible HTML settings form (if open) is updated by
            // SnykLanguageClientCustomTarget.OnHasAuthenticated calling
            // HtmlSettingsControl.Instance?.UpdateAuthToken; no IDE-side form updates here.

            await serviceProvider.ToolWindow.UpdateScreenStateAsync();
        }

        public async Task HandleFailedAuthenticationAsync(string errorMessage)
        {
            Logger.Information("Enter authenticate errorCallback");

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            // HideForAuthResult (not Hide) so a failure that beats the modal show to the UI thread
            // suppresses the pending show instead of leaving a dialog nothing will close.
            AuthDialog.HideForAuthResult();

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
