using System;
using System.IO;
using System.Text;
using Snyk.VisualStudio.Extension.UI.Html;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests.UI.Html
{
    public class WebView2NavigationPreparerTest : IDisposable
    {
        private readonly string _scratchDir;

        public WebView2NavigationPreparerTest()
        {
            _scratchDir = Path.Combine(Path.GetTempPath(), "snyk-webview2-tests-" + Guid.NewGuid().ToString("N"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_scratchDir))
            {
                Directory.Delete(_scratchDir, recursive: true);
            }
        }

        [Fact]
        public void Prepare_SmallHtml_ReturnsInlinePayload()
        {
            var preparer = new WebView2NavigationPreparer(_scratchDir);

            var payload = preparer.Prepare("<html><body>hi</body></html>");

            Assert.False(payload.IsFile);
            Assert.Equal("<html><body>hi</body></html>", payload.InlineHtml);
            Assert.Null(payload.FileUri);
        }

        [Fact]
        public void Prepare_HtmlOverInlineLimit_WritesFileAndReturnsFileUri()
        {
            var preparer = new WebView2NavigationPreparer(_scratchDir, inlineSizeLimitBytes: 1024);
            var bigHtml = "<html><body>" + new string('x', 2000) + "</body></html>";

            var payload = preparer.Prepare(bigHtml);

            Assert.True(payload.IsFile);
            Assert.Null(payload.InlineHtml);
            Assert.NotNull(payload.FileUri);
            Assert.Equal("file", payload.FileUri.Scheme);
            Assert.True(File.Exists(payload.FileUri.LocalPath), "Temp file was not written to disk");
            Assert.Equal(bigHtml, File.ReadAllText(payload.FileUri.LocalPath, Encoding.UTF8));
        }

        [Fact]
        public void Prepare_LargeHtml_CreatesScratchDirectoryIfMissing()
        {
            Assert.False(Directory.Exists(_scratchDir));
            var preparer = new WebView2NavigationPreparer(_scratchDir, inlineSizeLimitBytes: 16);

            preparer.Prepare("<html>" + new string('x', 100) + "</html>");

            Assert.True(Directory.Exists(_scratchDir));
        }

        [Fact]
        public void Prepare_SequentialLargeCalls_PreviousTempFileIsDeleted()
        {
            var preparer = new WebView2NavigationPreparer(_scratchDir, inlineSizeLimitBytes: 16);
            var first = preparer.Prepare("<html>" + new string('a', 100) + "</html>");
            Assert.True(File.Exists(first.FileUri.LocalPath));

            var second = preparer.Prepare("<html>" + new string('b', 100) + "</html>");

            Assert.False(File.Exists(first.FileUri.LocalPath));
            Assert.True(File.Exists(second.FileUri.LocalPath));
            Assert.NotEqual(first.FileUri.LocalPath, second.FileUri.LocalPath);
        }

        [Fact]
        public void Prepare_UsesUtf8ByteLengthNotCharLength()
        {
            // A 4-byte UTF-8 emoji counts as 4 bytes against the limit, not 2 chars.
            // Limit 40 bytes; 11 emojis × 4 bytes = 44 bytes → over.
            var preparer = new WebView2NavigationPreparer(_scratchDir, inlineSizeLimitBytes: 40);
            var html = string.Concat(new string[11] { "\U0001F600", "\U0001F600", "\U0001F600", "\U0001F600", "\U0001F600", "\U0001F600", "\U0001F600", "\U0001F600", "\U0001F600", "\U0001F600", "\U0001F600" });

            var payload = preparer.Prepare(html);

            Assert.True(payload.IsFile, "Expected emoji string to exceed the byte limit even though char count is small");
        }

        [Fact]
        public void Prepare_FollowedByInlineCall_StillCleansUpPreviousTempFile()
        {
            var preparer = new WebView2NavigationPreparer(_scratchDir, inlineSizeLimitBytes: 16);
            var first = preparer.Prepare("<html>" + new string('a', 100) + "</html>");
            Assert.True(File.Exists(first.FileUri.LocalPath));

            preparer.Prepare("<html>small</html>");

            Assert.False(File.Exists(first.FileUri.LocalPath));
        }

        [Fact]
        public void Dispose_DeletesPendingTempFile()
        {
            var preparer = new WebView2NavigationPreparer(_scratchDir, inlineSizeLimitBytes: 16);
            var payload = preparer.Prepare("<html>" + new string('a', 100) + "</html>");
            Assert.True(File.Exists(payload.FileUri.LocalPath));

            preparer.Dispose();

            Assert.False(File.Exists(payload.FileUri.LocalPath));
        }

        [Fact]
        public void Prepare_NullHtml_Throws()
        {
            var preparer = new WebView2NavigationPreparer(_scratchDir);

            Assert.Throws<ArgumentNullException>(() => preparer.Prepare(null));
        }
    }
}
