namespace Snyk.VisualStudio.Extension.Shared.CLI
{
    using System.Threading.Tasks;

    /// <summary>
    /// Describe Snyk CLI interface common methods.
    /// </summary>
    public interface ICli
    {
        /// <summary>
        /// Gets or sets a value indicating whether instance of <see cref="SnykConsoleRunner"/>.
        /// </summary>
        SnykConsoleRunner ConsoleRunner { get; set; }

        /// <summary>
        /// Run snyk test to scan for vulnerabilities.
        /// </summary>
        /// <param name="basePath">Path for run scan.</param>
        /// <returns><see cref="CliResult"/> object.</returns>
        Task<CliResult> ScanAsync(string basePath);

        /// <summary>
        /// Get Snyk API token from settings.
        /// </summary>
        /// <returns>API token string.</returns>
        string GetApiToken();

        /// <summary>
        /// Unsets the API token stored in the config file in <code>~/.config/configstore/snyk.json</code>
        /// </summary>
        void UnsetApiToken();

        /// <summary>
        /// Checks if the CLI executable exists.
        /// Checks the custom path specified in the settings, or the default path if the custom path is not specified.
        /// </summary>
        /// <returns>true if CLI executable is found, false otherwise.</returns>
        bool IsCliFileFound();
    }
}
