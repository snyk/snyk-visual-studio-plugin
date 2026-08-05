using Microsoft.VisualStudio.Shell;
using System.IO;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// The Visual Studio bound half of <see cref="StaticHtmlProvider"/>: loading the loading-state
    /// HTML has to be marshalled through the IDE's joinable task factory. Excluded from the
    /// cross-platform build (see docs/cross-platform-testing.md).
    /// </summary>
    public partial class StaticHtmlProvider
    {
        public async Task<string> GetInitHtmlAsync()
        {
            return await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                var assemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (assemblyLocation == null) return string.Empty;
                var path = Path.Combine(assemblyLocation, "Resources", "ScanSummaryInit.html");
                using (var stream = new StreamReader(path))
                {
                    var html = await stream.ReadToEndAsync();
                    return html;
                }
            });
        }
    }
}
