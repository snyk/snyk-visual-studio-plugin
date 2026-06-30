using System;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// Instance surface of <see cref="WebView2Host"/> used by the tool-window panels. Extracted
    /// as a seam so panel lifetime/disposal behaviour can be unit-tested against a mock without a
    /// real WebView2 runtime (static helpers such as <c>BuildUserDataFolder</c> stay on the
    /// concrete type).
    /// </summary>
    public interface IWebView2Host : IDisposable
    {
        /// <summary>Completes once <see cref="InitializeAsync"/> has finished and the control is ready.</summary>
        Task Ready { get; }

        Task InitializeAsync();

        Task NavigateAsync(string html);

        Task<string> ExecuteScriptAsync(string js);
    }
}
