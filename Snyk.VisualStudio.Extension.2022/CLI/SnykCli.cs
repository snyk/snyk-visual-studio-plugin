using System.IO;
using Serilog;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.CLI
{
    /// <summary>
    /// Incapsulate work logic with Snyk CLI.
    /// </summary>
    public class SnykCli : ICli
    {
        /// <summary>
        /// CLI name for Windows OS.
        /// </summary>
        public const string CliFileName = "snyk-win.exe";

        private static readonly ILogger Logger = LogManager.ForContext<SnykCli>();

        private ISnykOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCli"/> class.
        /// </summary>
        public SnykCli(ISnykOptions options, string ideVersion = "")
        {
            this.options = options;
        }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="ISnykOptions"/> (settings).
        /// </summary>
        public ISnykOptions Options
        {
            get { return this.options; }
            set { this.options = value; }
        }

        /// <summary>
        /// Get Snyk CLI file path.
        /// </summary>
        /// <returns>CLI path string.</returns>
        public static string GetSnykCliDefaultPath()
        {
            return Path.Combine(SnykDirectory.GetSnykAppDataDirectoryPath(), CliFileName);
        }

        /// <inheritdoc />
        public bool IsCliFileFound() => File.Exists(GetCliFilePath(this.Options.CliCustomPath));

        public string GetCliPath() => GetCliFilePath(this.options?.CliCustomPath);

        /// <summary>
        /// Gets the valid CLI path. When a custom CLI path is specified, it returns the custom path.
        /// When the custom CLI path is blank, it returns the default CLI path.
        /// </summary>
        /// <param name="customCliPath">The custom CLI path from the settings.</param>
        /// <returns>If <paramref name="customCliPath"/> is blank, the default path would be returned.</returns>
        // Blank, not just empty: the settings page stores whatever was typed, and a whitespace-only
        // value would otherwise count as a real custom path — File.Exists("   ") is always false, so
        // the CLI reads as missing and we install into an unusable location. Matches
        // ResolveBaseDownloadUrl and ResolveReleaseChannel, which guard the same way.
        public static string GetCliFilePath(string customCliPath) => string.IsNullOrWhiteSpace(customCliPath)
            ? SnykCli.GetSnykCliDefaultPath()
            : customCliPath.Trim();

        public static bool IsCliFileFound(string cliCustomPath)
        {
            var path = GetCliFilePath(cliCustomPath);
            return File.Exists(path);
        }
    }
}