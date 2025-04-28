using Snyk.VisualStudio.Extension.Service;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    [ComVisible(true)]
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
            if (string.IsNullOrEmpty(link) || !link.StartsWith("http"))
                return;
            Process.Start(link);
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

            string[] separator = { "@|@" };
            var args = value.Split(separator, StringSplitOptions.None);
                if (args.Length > 1)
                {
                    var folderURI = args[0];
                    var fileURI = args[1];
                    var issueID = args[2];
                    await LanguageClientHelper.LanguageClientManager().SendCodeFixDiffsAsync(folderURI, fileURI, issueID, SnykVSPackage.Instance.DisposalToken);
                }
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

    }
}
