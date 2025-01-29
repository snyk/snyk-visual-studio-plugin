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

        public void ToggleDelta(bool isEnabled)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                this.serviceProvider.Options.EnableDeltaFindings = isEnabled;
                this.serviceProvider.SnykOptionsManager.Save(this.serviceProvider.Options);
                await LanguageClientHelper.LanguageClientManager().DidChangeConfigurationAsync(SnykVSPackage.Instance.DisposalToken);

            }).FireAndForget();
        }
    }
}