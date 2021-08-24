namespace Snyk.VisualStudio.Extension.CLI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Settings;

    /// <summary>
    /// Incapsulate work logic with Snyk CLI.
    /// </summary>
    public class SnykCli
    {
        /// <summary>
        /// CLI name for Windows OS.
        /// </summary>
        public const string CliFileName = "snyk-win.exe";

        private ISnykOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykCli"/> class.
        /// </summary>
        public SnykCli() => this.ConsoleRunner = new SnykConsoleRunner();

        /// <summary>
        /// Gets or sets a value indicating whether instance of <see cref="SnykConsoleRunner"/>.
        /// </summary>
        public SnykConsoleRunner ConsoleRunner { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether logger instance.
        /// </summary>
        public SnykActivityLogger Logger { get; set; }

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

        /// <summary>
        /// Get Snyk CLI file path.
        /// </summary>
        /// <returns>CLI path string.</returns>
        public static string GetSnykCliPath() => Path.Combine(SnykDirectory.GetSnykAppDataDirectoryPath(), CliFileName);

        /// <summary>
        /// Check is CLI file exists in $UserDirectory\.AppData\Snyk.
        /// </summary>
        /// <returns>True if CLI file exists.</returns>
        public static bool IsCliExists() => File.Exists(GetSnykCliPath());

        /// <summary>
        /// Get Snyk API token from settings.
        /// </summary>
        /// <returns>API token string.</returns>
        public string GetApiToken() => this.ConsoleRunner.Run(GetSnykCliPath(), "config get api");

        /// <summary>
        /// Call Snyk CLI auth for authentication. This will open authentication web page and store token in config file.
        /// </summary>
        /// <returns>Snyk API token.</returns>
        public string Authenticate() => this.ConsoleRunner.Run(GetSnykCliPath(), "auth");

        /// <summary>
        /// Run snyk test to scan for vulnerabilities.
        /// </summary>
        /// <param name="basePath">Path for run scan.</param>
        /// <returns><see cref="CliResult"/> object.</returns>
        public CliResult Scan(string basePath)
        {
            this.Logger?.LogInformation("Enter Scan() method");
            this.Logger?.LogInformation($"Base path is {basePath}");

            string cliPath = GetSnykCliPath();

            this.Logger?.LogInformation($"CLI path is {cliPath}");

            this.ConsoleRunner.CreateProcess(cliPath, this.BuildArguments());

            this.Logger?.LogInformation("Adding token");

            if (!string.IsNullOrEmpty(this.Options.ApiToken))
            {
                this.Logger?.LogInformation("Token added from Options");

                this.ConsoleRunner.Process.StartInfo.EnvironmentVariables["SNYK_TOKEN"] = this.Options.ApiToken;
            }

            this.ConsoleRunner.Process.StartInfo.WorkingDirectory = basePath;

            this.Logger?.LogInformation("Start run console process");

            string consoleResult = this.ConsoleRunner.Execute();

            this.Logger?.LogInformation("Leave Scan() method");

            this.Logger?.LogInformation("Leave Scan() method");

            return this.ConvertRawCliStringToCliResult(consoleResult);
        }

        /// <summary>
        /// Build arguments (options) for snyk cli depending on user settings.
        /// </summary>
        /// <returns>arguments string.</returns>
        public string BuildArguments()
        {
            this.Logger?.LogInformation("Enter BuildArguments method");

            var arguments = new List<string>();

            arguments.Add("--json");
            arguments.Add("test");

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

            if (!string.IsNullOrEmpty(this.Options.AdditionalOptions))
            {
                arguments.Add($"{this.Options.AdditionalOptions}");
            }

            if (this.Options.IsScanAllProjects)
            {
                arguments.Add("--all-projects");
            }

            if (!this.Options.UsageAnalyticsEnabled)
            {
                arguments.Add("--DISABLE_ANALYTICS");
            }

            string cliOptions = string.Join(" ", arguments.ToArray());

            this.Logger?.LogInformation($"Result CLI options {cliOptions}");
            this.Logger?.LogInformation("Leave BuildArguments method");

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
        public CliResult ConvertRawCliStringToCliResult(string rawResult)
        {
            if (rawResult.First() == '[')
            {
                return new CliResult
                {
                    CliVulnerabilitiesList = Json.Deserialize<List<CliVulnerabilities>>(rawResult),
                };
            } else if (rawResult.First() == '{')
            {
                if (this.IsSuccessCliJsonString(rawResult))
                {
                    var cliVulnerabilities = Json.Deserialize<CliVulnerabilities>(rawResult);

                    var cliVulnerabilitiesList = new List<CliVulnerabilities>();
                    cliVulnerabilitiesList.Add(cliVulnerabilities);

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
        public bool IsSuccessCliJsonString(string json) => json.Contains("\"vulnerabilities\":") && !json.Contains("\"error\":");
    }
}