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
    /// accept up to ~2&nbsp;MB of HTML; oversized content gets spilled to a temp file under a
    /// caller-supplied scratch directory.
    /// </summary>
    /// <remarks>
    /// Temp-file deletion is deferred by one navigation cycle: the file from the previous Prepare
    /// call only becomes eligible for deletion when the call AFTER that one runs. By then,
    /// WebView2 has navigated away from the first file and any in-flight file handles are
    /// released, so deletion is race-free. The remaining files are swept at construction (clearing
    /// leftovers from a crashed session) and at <see cref="Dispose"/>.
    /// </remarks>
    public sealed class WebView2NavigationPreparer : IDisposable
    {
        public const int DefaultInlineSizeLimitBytes = 2_000_000;

        private static readonly ILogger Logger = LogManager.ForContext<WebView2NavigationPreparer>();

        private readonly string _scratchDirectory;
        private readonly int _inlineSizeLimitBytes;

        // Two-deep retirement queue: the "current" temp file is the one WebView2 is currently
        // navigating to (may still have an open file handle on it). The "previous" file is the
        // one WebView2 has moved off — safe to delete on the next Prepare.
        private string _previousTempFile;
        private string _currentTempFile;

        public WebView2NavigationPreparer(string scratchDirectory, int inlineSizeLimitBytes = DefaultInlineSizeLimitBytes)
        {
            if (string.IsNullOrEmpty(scratchDirectory)) throw new ArgumentException("Scratch directory is required", nameof(scratchDirectory));
            if (inlineSizeLimitBytes <= 0) throw new ArgumentOutOfRangeException(nameof(inlineSizeLimitBytes));

            _scratchDirectory = scratchDirectory;
            _inlineSizeLimitBytes = inlineSizeLimitBytes;

            // Clean up files left behind by a previous (possibly crashed) session.
            SweepScratchDirectory();
        }

        public NavigationPayload Prepare(string html)
        {
            if (html == null) throw new ArgumentNullException(nameof(html));

            var byteCount = Encoding.UTF8.GetByteCount(html);
            if (byteCount <= _inlineSizeLimitBytes)
            {
                return NavigationPayload.Inline(html);
            }

            Directory.CreateDirectory(_scratchDirectory);

            // _previousTempFile has survived a full navigation cycle: WebView2 navigated to
            // _currentTempFile after _previousTempFile was set, so any handle on _previousTempFile
            // has been released. Safe to delete.
            TryDeleteFile(_previousTempFile);
            _previousTempFile = _currentTempFile;

            var path = Path.Combine(_scratchDirectory, "navigate-" + Guid.NewGuid().ToString("N") + ".html");
            System.IO.File.WriteAllText(path, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            _currentTempFile = path;

            return NavigationPayload.File(new Uri(path));
        }

        public void Dispose()
        {
            SweepScratchDirectory();
        }

        private void SweepScratchDirectory()
        {
            try
            {
                if (Directory.Exists(_scratchDirectory))
                {
                    foreach (var file in Directory.EnumerateFiles(_scratchDirectory))
                    {
                        TryDeleteFile(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to enumerate scratch directory {Path}", _scratchDirectory);
            }
            finally
            {
                _previousTempFile = null;
                _currentTempFile = null;
            }
        }

        private static void TryDeleteFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to delete WebView2 navigation temp file {Path}", path);
            }
        }
    }
}
