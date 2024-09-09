using Snyk.Common;
using Snyk.VisualStudio.Extension.Service;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Serilog;

namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    [ComVisible(true)]
    public class SnykScriptManager
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykScriptManager>();

        public void OpenFileInEditor(string filePath, string startLine, string endLine, string startCharacter, string endCharacter)
        {
            try
            {
                Task.Run(async () =>
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
    }
}