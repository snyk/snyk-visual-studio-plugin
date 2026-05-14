using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Serilog;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// Wires a WPF <see cref="WebView2"/> control to the JS↔C# bridge used by the LS-authored
    /// HTML pages: registers the <see cref="WebView2BridgeBindings"/> shim, routes
    /// <c>WebMessageReceived</c> through the supplied <see cref="WebView2MessageDispatcher"/>,
    /// and handles the &gt;2&nbsp;MB <c>NavigateToString</c> spill-to-disk fallback. The underlying
    /// <see cref="CoreWebView2Environment"/> is shared across every host in the process via
    /// <see cref="WebView2EnvironmentProvider"/>.
    /// </summary>
    /// <remarks>
    /// This class is mostly orchestration over the WebView2 SDK and isn't directly unit-tested;
    /// the testable pieces (<see cref="WebView2MessageDispatcher"/>,
    /// <see cref="WebView2BridgeBindings"/>, <see cref="WebView2NavigationPreparer"/>)
    /// have their own test suites. End-to-end behaviour is covered by the panel migrations
    /// (Phase 3-5 of IDE-1707).
    /// </remarks>
    public sealed class WebView2Host : IDisposable
    {
        private static readonly ILogger Logger = LogManager.ForContext<WebView2Host>();

        private readonly WebView2 _webView;
        private readonly WebView2MessageDispatcher _dispatcher;
        private readonly WebView2NavigationPreparer _preparer;
        private readonly IReadOnlyList<string> _additionalInitScripts;
        private readonly bool _enableDeveloperTools;
        private readonly TaskCompletionSource<bool> _readyTcs =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private bool _initialized;

        /// <summary>
        /// Constructs the host. <paramref name="scratchDirectory"/> is the panel-specific folder
        /// for oversized-HTML spill files (see <see cref="WebView2NavigationPreparer"/>);
        /// use <see cref="WebView2EnvironmentProvider.GetScratchDirectory"/> to obtain one.
        /// <paramref name="additionalInitScripts"/> are registered via
        /// <c>AddScriptToExecuteOnDocumentCreatedAsync</c> after the bridge bindings, before
        /// the first navigation — for example, <see cref="ExecuteCommandBridge.BuildClientScript"/>
        /// which redefines <c>window.__ideExecuteCommand__</c> with its callback-id roundtrip.
        /// <paramref name="enableDeveloperTools"/> turns on the Chromium DevTools (F12) for the
        /// hosted control — useful for the debug subclasses, off in production.
        /// </summary>
        public WebView2Host(
            WebView2 webView,
            WebView2MessageDispatcher dispatcher,
            string scratchDirectory,
            IEnumerable<string> additionalInitScripts = null,
            bool enableDeveloperTools = false)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            if (string.IsNullOrEmpty(scratchDirectory)) throw new ArgumentException("Scratch directory is required", nameof(scratchDirectory));

            _additionalInitScripts = (additionalInitScripts ?? Enumerable.Empty<string>()).ToArray();
            _enableDeveloperTools = enableDeveloperTools;
            _preparer = new WebView2NavigationPreparer(scratchDirectory);
        }

        /// <summary>
        /// Completes once <see cref="InitializeAsync"/> has finished and the control is
        /// ready for navigation / script execution. Faults with the init exception on failure.
        /// </summary>
        public Task Ready => _readyTcs.Task;

        public async Task InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                var environment = await WebView2EnvironmentProvider.GetAsync();
                await _webView.EnsureCoreWebView2Async(environment);

                ConfigureSettings(_webView.CoreWebView2.Settings, _enableDeveloperTools);

                await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                    WebView2BridgeBindings.BuildScript());

                foreach (var script in _additionalInitScripts)
                {
                    await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
                }

                _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                _readyTcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "WebView2 initialization failed");
                _readyTcs.TrySetException(ex);
                throw;
            }
        }

        public async Task NavigateAsync(string html)
        {
            await Ready.ConfigureAwait(true);

            var payload = _preparer.Prepare(html);
            if (payload.IsFile)
            {
                _webView.Source = payload.FileUri;
            }
            else
            {
                _webView.NavigateToString(payload.InlineHtml);
            }
        }

        public async Task<string> ExecuteScriptAsync(string js)
        {
            await Ready.ConfigureAwait(true);
            return await _webView.CoreWebView2.ExecuteScriptAsync(js);
        }

        public void Dispose()
        {
            if (_webView?.CoreWebView2 != null)
            {
                _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            }
            _preparer.Dispose();
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            _dispatcher.Dispatch(e.WebMessageAsJson);
        }

        private static void ConfigureSettings(CoreWebView2Settings settings, bool enableDeveloperTools)
        {
            settings.AreDefaultContextMenusEnabled = enableDeveloperTools;
            settings.IsStatusBarEnabled = false;
            settings.AreDevToolsEnabled = enableDeveloperTools;
            settings.IsZoomControlEnabled = false;
            settings.AreBrowserAcceleratorKeysEnabled = false;
        }
    }
}
