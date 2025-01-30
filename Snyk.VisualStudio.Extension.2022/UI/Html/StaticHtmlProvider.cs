using Microsoft.VisualStudio.Shell;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    public class StaticHtmlProvider : BaseHtmlProvider
    {
        private static StaticHtmlProvider _instance;

        public static StaticHtmlProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new StaticHtmlProvider();
                }
                return _instance;
            }
        }
        public override string GetInitScript()
        {
          return @"";
        }

        public async Task<string> GetInitHtmlAsync()
        {
            return await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                var assemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (assemblyLocation == null) return string.Empty;
                var path = Path.Combine(assemblyLocation, "Resources", "LoadingSummary.html");
                using (var stream = new StreamReader(path))
                {
                    var html = await stream.ReadToEndAsync();
                    return html;
                }
            });
        }
    }
}