using System;
using System.IO;
using System.Text;
using Serilog;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    public sealed class NavigationPayload
    {
        private NavigationPayload(string inlineHtml, Uri fileUri)
        {
            InlineHtml = inlineHtml;
            FileUri = fileUri;
        }

        public string InlineHtml { get; }
        public Uri FileUri { get; }
        public bool IsFile => FileUri != null;

        public static NavigationPayload Inline(string html) => new NavigationPayload(html, null);
        public static NavigationPayload File(Uri uri) => new NavigationPayload(null, uri);
    }

    /// <summary>
    /// Picks between <c>CoreWebView2.NavigateToString</c> and writing HTML to a temp file and
    /// navigating via <c>Source = new Uri(...)</c>. <c>NavigateToString</c> is documented to
    /// accept up to ~2 MB of HTML; oversized content gets spilled to a temp file under a
    /// caller-supplied scratch directory. Each call cleans up the previous temp file.
    /// </summary>
    public sealed class WebView2NavigationPreparer : IDisposable
    {
        public const int DefaultInlineSizeLimitBytes = 2_000_000;

        private static readonly ILogger Logger = LogManager.ForContext<WebView2NavigationPreparer>();

        private readonly string _scratchDirectory;
        private readonly int _inlineSizeLimitBytes;
        private string _previousTempFile;

        public WebView2NavigationPreparer(string scratchDirectory, int inlineSizeLimitBytes = DefaultInlineSizeLimitBytes)
        {
            if (string.IsNullOrEmpty(scratchDirectory)) throw new ArgumentException("Scratch directory is required", nameof(scratchDirectory));
            if (inlineSizeLimitBytes <= 0) throw new ArgumentOutOfRangeException(nameof(inlineSizeLimitBytes));

            _scratchDirectory = scratchDirectory;
            _inlineSizeLimitBytes = inlineSizeLimitBytes;
        }

        public NavigationPayload Prepare(string html)
        {
            if (html == null) throw new ArgumentNullException(nameof(html));

            var byteCount = Encoding.UTF8.GetByteCount(html);
            if (byteCount <= _inlineSizeLimitBytes)
            {
                CleanupPreviousTempFile();
                return NavigationPayload.Inline(html);
            }

            Directory.CreateDirectory(_scratchDirectory);
            CleanupPreviousTempFile();

            var path = Path.Combine(_scratchDirectory, "navigate-" + Guid.NewGuid().ToString("N") + ".html");
            System.IO.File.WriteAllText(path, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            _previousTempFile = path;

            return NavigationPayload.File(new Uri(path));
        }

        public void Dispose()
        {
            CleanupPreviousTempFile();
        }

        private void CleanupPreviousTempFile()
        {
            if (string.IsNullOrEmpty(_previousTempFile)) return;

            try
            {
                if (System.IO.File.Exists(_previousTempFile))
                {
                    System.IO.File.Delete(_previousTempFile);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to delete previous WebView2 navigation temp file {Path}", _previousTempFile);
            }
            finally
            {
                _previousTempFile = null;
            }
        }
    }
}
