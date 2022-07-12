namespace Snyk.VisualStudio.Extension.Shared.CLI
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.Settings;

    /// <summary>
    /// Incapsulate work logic with Snyk CLI.
    /// </summary>
    public class SnykCli : ICli
    {
        /// <summary>
        /// CLI name for Windows OS.
        /// </summary>
        public const string CliFileName = "snyk-win.exe";

        private const string ApiEnvironmentVariableName = "SNYK_API";

        private static readonly ILogger Logger = LogManager.ForContext<SnykCli>();

        private ISnykOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCli"/> class.
        /// </summary>
        public SnykCli(ISnykOptions options)
        {
            this.ConsoleRunner = new SnykConsoleRunner();
            this.options = options;
        }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="ISnykOptions"/> (settings).
        /// </summary>
        public ISnykOptions Options
        {
            get
            {
                return this.options;
            }
            set
            {
                this.options = value;
            }
        }

        /// <inheritdoc/>
        public SnykConsoleRunner ConsoleRunner { get; set; }

        /// <summary>
        /// Get Snyk CLI file path.
        /// </summary>
        /// <returns>CLI path string.</returns>
        public static string GetSnykCliDefaultPath()
        {
            return Path.Combine(SnykDirectory.GetSnykAppDataDirectoryPath(), CliFileName);
        }

        /// <summary>
        /// Check is CLI file exists.
        /// </summary>
        /// <param name="customCliPath">Custom CLI path. If null or empty, the default path from AppData will be used.</param>
        /// <returns>True if CLI file exists.</returns>
        public static bool DoesCliExist(string customCliPath)
        {
            var path = string.IsNullOrEmpty(customCliPath) ? GetSnykCliDefaultPath() : customCliPath;
            return File.Exists(path);
        }

        /// <summary>
        /// Safely get Snyk API token from settings.
        /// </summary>
        /// <returns>API token string.</returns>
        public string GetApiToken()
        {
            string apiToken;

            try
            {
                apiToken = this.GetApiTokenOrThrowException();
            }
            catch (InvalidTokenException e)
            {
                Logger.Error(e, "Error on get api token via cli for settings");

                apiToken = string.Empty;
            }

            return apiToken;
        }

        /// <inheritdoc/>
        public void UnsetApiToken() => this.ConsoleRunner.Run(this.GetCliPath(), "config unset api");

        /// <summary>
        /// Try get Snyk API token from snyk cli config or throw <see cref="InvalidTokenException"/>.
        /// </summary>
        /// <returns>API token string.</returns>
        public string GetApiTokenOrThrowException()
        {
            string apiToken = this.ConsoleRunner.Run(this.GetCliPath(), "config get api");

            if (!Guid.IsValid(apiToken))
            {
                throw new InvalidTokenException(string.IsNullOrEmpty(apiToken) ? string.Empty : apiToken);
            }

            return apiToken;
        }

        /// <summary>
        /// Call Snyk CLI auth for authentication. This will open authentication web page and store token in config file.
        /// </summary>
        /// <returns>Snyk API token.</returns>
        public string Authenticate()
        {
            var args = new List<string> { "auth" };
            if (this.Options.IgnoreUnknownCA)
            {
                args.Add("--insecure");
            }

            var environmentVariables = new StringDictionary();
            if (!string.IsNullOrEmpty(this.Options.CustomEndpoint))
            {
                environmentVariables.Add(ApiEnvironmentVariableName, this.Options.CustomEndpoint);
            }

            return this.ConsoleRunner.Run(this.GetCliPath(), string.Join(" ", args), environmentVariables);
        }

        /// <inheritdoc/>
        public async Task<CliResult> ScanAsync(string basePath)
        {
            Logger.Information("Path to scan {BasePath}", basePath);

            var cliPath = this.GetCliPath();

            Logger.Information("CLI path is {CliPath}", cliPath);

            var arguments = await this.BuildScanArgumentsAsync();

            this.ConsoleRunner.CreateProcess(cliPath, arguments, this.BuildScanEnvironmentVariables(), basePath);

            Logger.Information("Start run console process");

            var consoleResult = this.ConsoleRunner.Execute();

            Logger.Information("Start convert console string result to CliResult and return value");

            return ConvertRawCliStringToCliResult(consoleResult);
        }

        private string GetCliPath()
        {
            var snykCliCustomPath = this.options?.CliCustomPath;
            var cliPath = string.IsNullOrEmpty(snykCliCustomPath) ? GetSnykCliDefaultPath() : snykCliCustomPath;
            return cliPath;
        }

        public StringDictionary BuildScanEnvironmentVariables()
        {
            var environmentVariables = new StringDictionary();
            if (!string.IsNullOrEmpty(this.Options.ApiToken))
            {
                environmentVariables.Add("SNYK_TOKEN", this.Options.ApiToken);
                Logger.Information("Token added from Options");
            }

            if (!string.IsNullOrEmpty(this.Options.CustomEndpoint))
            {
                environmentVariables.Add(ApiEnvironmentVariableName, this.Options.CustomEndpoint);
                Logger.Information("Custom endpoint added from Options");
            }

            return environmentVariables;
        }

        /// <summary>
        /// Build arguments (options) for snyk cli depending on user settings.
        /// </summary>
        /// <returns>arguments string.</returns>
        public async Task<string> BuildScanArgumentsAsync()
        {
            Logger.Information("Enter BuildArguments method");

            var arguments = new List<string>
            {
                "--json",
                "test",
            };

            if (!string.IsNullOrEmpty(this.Options.CustomEndpoint))
            {
                arguments.Add($"--API={this.Options.CustomEndpoint}");
            }

            if (this.Options.IgnoreUnknownCA)
            {
                arguments.Add("--insecure");
            }

            if (!string.IsNullOrEmpty(this.Options.Organization))
            {
                arguments.Add($"--org={this.Options.Organization}");
            }

            var additionalOptions = await this.Options.GetAdditionalOptionsAsync();

            if (!string.IsNullOrEmpty(additionalOptions))
            {
                arguments.Add($"{additionalOptions}");
            }

            var isScanAllProjects = await this.Options.IsScanAllProjectsAsync();

            if (isScanAllProjects)
            {
                arguments.Add("--all-projects");
            }

            if (!this.Options.UsageAnalyticsEnabled)
            {
                arguments.Add("--DISABLE_ANALYTICS");
            }

            string cliOptions = string.Join(" ", arguments.ToArray());

            Logger.Information("Result CLI options {CliOptions}", cliOptions);
            Logger.Information("Leave BuildArguments method");

            return cliOptions;
        }

        /// <summary>
        /// Convert raw json string to <see cref="CliResult"/> object.
        /// Check is json object is array. If it's array of cli vulnerability objects it will create <see cref="CliVulnerabilities"/> list.
        /// If json string is single object it will create <see cref="CliVulnerabilities"/> object.
        /// If json string is error it will create <see cref="CliError"/> object.
        /// </summary>
        /// <param name="rawResult">Json string.</param>
        /// <returns>Result <see cref="CliResult"/> object.</returns>
        public static CliResult ConvertRawCliStringToCliResult(string rawResult)
        {
            if (rawResult.First() == '[')
            {
                return new CliResult
                {
                    CliVulnerabilitiesList = Json.Deserialize<List<CliVulnerabilities>>(rawResult),
                };
            } else if (rawResult.First() == '{')
            {
                if (IsSuccessCliJsonString(rawResult))
                {
                    var cliVulnerabilities = Json.Deserialize<CliVulnerabilities>(rawResult);

                    var cliVulnerabilitiesList = new List<CliVulnerabilities> { cliVulnerabilities };

                    return new CliResult
                    {
                        CliVulnerabilitiesList = cliVulnerabilitiesList,
                    };
                } else
                {
                    return new CliResult
                    {
                        Error = Json.Deserialize<CliError>(rawResult),
                    };
                }
            } else
            {
                return new CliResult
                {
                    Error = new CliError
                    {
                        IsSuccess = false,
                        Message = rawResult,
                        Path = string.Empty,
                    },
                };
            }
        }

        /// <summary>
        /// Check is json string contains error or it contains vulnerabilities object(s).
        /// </summary>
        /// <param name="json">Source json string.</param>
        /// <returns>True if json string contains vulnerabilities object(s).</returns>
        public static bool IsSuccessCliJsonString(string json) => json.Contains("\"vulnerabilities\":") && !json.Contains("\"error\":");
    }
}