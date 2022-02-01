namespace Snyk.VisualStudio.Extension.Shared.CLI
{
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
        CliResult Scan(string basePath);

        /// <summary>
        /// Get Snyk API token from settings.
        /// </summary>
        /// <returns>API token string.</returns>
        string GetApiToken();
    }
}
