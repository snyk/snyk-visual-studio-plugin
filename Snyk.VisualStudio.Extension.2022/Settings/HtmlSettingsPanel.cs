// ABOUTME: WinForms user control for HTML-based settings using WebBrowser control
// ABOUTME: Displays configuration HTML from Language Server with IE11 fallback support

using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.UI.Html;

namespace Snyk.VisualStudio.Extension.Settings
{
    public partial class HtmlSettingsPanel : UserControl
    {
        private readonly ISnykOptions options;
        private readonly IJsonRpc languageServerRpc;
        private ConfigScriptingBridge scriptingBridge;
        private WebBrowser webBrowser;
        private Label loadingLabel;
        private Label errorLabel;
        private bool isModified;
        private bool isInitialized;

        public bool IsModified => isModified;

        public HtmlSettingsPanel() : this(null, null)
        {
            // Parameterless constructor for designer
        }

        public HtmlSettingsPanel(ISnykOptions options, IJsonRpc languageServerRpc)
        {
            this.options = options;
            this.languageServerRpc = languageServerRpc;

            InitializeComponent();
            CreateControls();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "HtmlSettingsPanel";
            this.Size = new System.Drawing.Size(800, 600);
            this.ResumeLayout(false);
        }

        private void CreateControls()
        {
            // Loading label
            loadingLabel = new Label
            {
                Text = "Loading Snyk settings...",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Visible = true
            };
            this.Controls.Add(loadingLabel);

            // Error label
            errorLabel = new Label
            {
                ForeColor = System.Drawing.Color.Red,
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Visible = false
            };
            this.Controls.Add(errorLabel);

            // WebBrowser
            webBrowser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                Visible = false,
                ScriptErrorsSuppressed = false, // Show script errors for debugging
                AllowNavigation = false, // Prevent navigation away
                IsWebBrowserContextMenuEnabled = false, // Disable right-click menu
                WebBrowserShortcutsEnabled = false
            };
            this.Controls.Add(webBrowser);

            // Handle load completion
            webBrowser.DocumentCompleted += OnDocumentCompleted;
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!isInitialized && options != null)
            {
                await InitializeAsync();
                isInitialized = true;
            }
        }

        private async Task InitializeAsync()
        {
            try
            {
                // Set up scripting bridge
                scriptingBridge = new ConfigScriptingBridge(
                    options,
                    onModified: () => isModified = true,
                    onReset: () => isModified = false,
                    onSaveComplete: () => { /* Post-apply logic */ }
                );

                webBrowser.ObjectForScripting = scriptingBridge;

                // Load HTML content
                var html = await GetHtmlContentAsync();

                if (string.IsNullOrEmpty(html))
                {
                    ShowError("Failed to load settings panel");
                    return;
                }

                // Apply theme
                html = HtmlResourceLoader.ApplyTheme(html);

                // Navigate to HTML
                webBrowser.DocumentText = html;
            }
            catch (Exception ex)
            {
                ShowError($"Error initializing settings panel: {ex.Message}");
            }
        }

        private void OnDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                // Ensure scripting bridge is set
                webBrowser.ObjectForScripting = scriptingBridge;

                // Set IE11 document mode for better compatibility
                SetBrowserFeatureControl();

                // Hide loading, show WebBrowser
                loadingLabel.Visible = false;
                webBrowser.Visible = true;
            }
            catch (Exception ex)
            {
                ShowError($"Error loading settings content: {ex.Message}");
            }
        }

        private async Task<string> GetHtmlContentAsync()
        {
            // Try to get HTML from Language Server (with retries)
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var lsHtml = await LanguageServerCommands.GetConfigHtmlAsync(
                        languageServerRpc,
                        System.Threading.CancellationToken.None);
                    if (!string.IsNullOrEmpty(lsHtml))
                        return lsHtml;
                }
                catch
                {
                    // Ignore and retry
                }

                if (i < 2)
                    await Task.Delay(1000);
            }

            // Fall back to embedded HTML
            return HtmlResourceLoader.LoadFallbackHtml(options);
        }

        private void ShowError(string message)
        {
            loadingLabel.Visible = false;
            webBrowser.Visible = false;
            errorLabel.Text = message;
            errorLabel.Visible = true;
        }

        public async Task ApplyAsync()
        {
            if (!IsModified) return;

            try
            {
                // Call JavaScript function to collect and save config
                webBrowser.Document?.InvokeScript("getAndSaveIdeConfig");
                isModified = false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to apply settings: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Set IE11 feature control for modern rendering mode.
        /// This ensures the WebBrowser control uses IE11 mode instead of IE7 compatibility mode.
        /// </summary>
        private void SetBrowserFeatureControl()
        {
            try
            {
                var appName = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                SetBrowserFeatureControlKey("FEATURE_BROWSER_EMULATION", appName, 11001); // IE11 mode
                SetBrowserFeatureControlKey("FEATURE_DISABLE_NAVIGATION_SOUNDS", appName, 1);
                SetBrowserFeatureControlKey("FEATURE_SCRIPTURL_MITIGATION", appName, 1);
            }
            catch
            {
                // Ignore errors - registry access might be restricted
            }
        }

        private void SetBrowserFeatureControlKey(string feature, string appName, int value)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                    $@"Software\Microsoft\Internet Explorer\Main\FeatureControl\{feature}",
                    Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    key?.SetValue(appName, value, Microsoft.Win32.RegistryValueKind.DWord);
                }
            }
            catch
            {
                // Silently ignore registry errors
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                webBrowser?.Dispose();
                loadingLabel?.Dispose();
                errorLabel?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
