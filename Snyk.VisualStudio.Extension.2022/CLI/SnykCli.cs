using System.IO;
using Serilog;
using Snyk.Common;
using Snyk.Common.Settings;
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
        public bool IsCliFileFound()
        {
            var customPath = this.Options.CliCustomPath;
            var path = string.IsNullOrEmpty(customPath) ? GetSnykCliDefaultPath() : customPath;
            return File.Exists(path);
        }

        public string GetCliPath()
        {
            var snykCliCustomPath = this.options?.CliCustomPath;
            var cliPath = string.IsNullOrEmpty(snykCliCustomPath) ? GetSnykCliDefaultPath() : snykCliCustomPath;
            return cliPath;
        }

        /// <summary>
        /// Gets the valid CLI path. When a custom CLI path is specified, it returns the custom path.
        /// When the Custom CLI path is null or empty, it returns the default CLI path.
        /// </summary>
        /// <param name="customCliPath">The custom CLI path from the settings.</param>
        /// <returns>If <paramref name="customCliPath"/> is null or empty, the default path would be returned.</returns>
        public static string GetCliFilePath(string customCliPath) => string.IsNullOrEmpty(customCliPath)
            ? SnykCli.GetSnykCliDefaultPath()
            : customCliPath;

        public static bool IsCliFileFound(string cliCustomPath)
        {
            var path = GetCliFilePath(cliCustomPath);
            return File.Exists(path);
        }
    }
}