using Snyk.VisualStudio.Extension.Service;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.VisualStudio.Extension.Extension;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    public class SnykScriptManager
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykScriptManager>();
        private readonly ISnykServiceProvider serviceProvider;

        public SnykScriptManager(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }
        public void OpenFileInEditor(string filePath, string startLine, string endLine, string startCharacter, string endCharacter)
        {
            try
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    VsCodeService.Instance.OpenAndNavigate(
                        filePath,
                        int.Parse(startLine),
                        int.Parse(startCharacter),
                        int.Parse(endLine),
                        int.Parse(endCharacter));
                }).FireAndForget();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error on open and navigate to source code");
            }
        }

        public void OpenLink(string link)
        {
            // Only launch absolute http/https URLs. The old StartsWith("http") check also matched
            // e.g. "httpx://…" and handed arbitrary strings to the shell (UseShellExecute), which
            // could launch any registered URI handler. Validate before Process.Start.
            if (!UriExtensions.IsValidWebUrl(link))
            {
                Logger.Warning("Ignoring OpenLink request that is not an absolute http/https URL");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to open link");
            }
        }

        public void EnableDelta(bool isEnabled)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                this.serviceProvider.Options.EnableDeltaFindings = isEnabled;
                this.serviceProvider.SnykOptionsManager.Save(this.serviceProvider.Options);
                await LanguageClientHelper.LanguageClientManager().DidChangeConfigurationAsync(SnykVSPackage.Instance.DisposalToken);

            }).FireAndForget();
        }
        public void GenerateFixes(string value)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () => {
            await LanguageClientHelper.LanguageClientManager().SendCodeFixDiffsAsync(value, SnykVSPackage.Instance.DisposalToken);
            }).FireAndForget();
        }
        public void ApplyFixDiff(string fixID)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () => {
                await LanguageClientHelper.LanguageClientManager().SendApplyFixDiffsAsync(fixID, SnykVSPackage.Instance.DisposalToken);
            }).FireAndForget();
        }
        public void SubmitIgnoreRequest(string issueId, string ignoreType, string ignoreReason, string ignoreExpirationDate)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () => {
                await LanguageClientHelper.LanguageClientManager().SubmitIgnoreRequestAsync("create", issueId, ignoreType, ignoreReason, ignoreExpirationDate, SnykVSPackage.Instance.DisposalToken);
            }).FireAndForget();
        }
        public void FocusToolWindow()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                // Using .Show() as .Focus() didn't work on anything we tried.
                this.serviceProvider.ToolWindow.Show();
            }).FireAndForget();
        }
    }
}
