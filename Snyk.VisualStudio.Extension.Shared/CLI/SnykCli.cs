﻿namespace Snyk.VisualStudio.Extension.Shared.CLI
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Security.Authentication;
    using System.Threading.Tasks;
    using Serilog;
    using Snyk.Common;
    using Snyk.Common.Authentication;
    using Snyk.Common.Service;
    using Snyk.Common.Settings;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using Snyk.VisualStudio.Extension.Shared.Settings;
    using Snyk.VisualStudio.Extension.Shared.UI.Notifications;

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
        public SnykCli(ISnykOptions options, string ideVersion = "")
        {
            this.ConsoleRunner = new SnykConsoleRunner(ideVersion);
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

        private string GetTokenKey()
        {
            var apiEndpointResolver = new ApiEndpointResolver(this.options);

            var tokenKey = "api";
            if (apiEndpointResolver.AuthenticationMethod == AuthenticationType.OAuth)
            {
                tokenKey = "INTERNAL_OAUTH_TOKEN_STORAGE";
            }

            return tokenKey;
        }

        /// <inheritdoc/>
        public void UnsetApiToken() => this.ConsoleRunner.Run(this.GetCliPath(), "config unset " + this.GetTokenKey());

        /// <inheritdoc />
        public bool IsCliFileFound()
        {
            var customPath = this.Options.CliCustomPath;
            var path = string.IsNullOrEmpty(customPath) ? GetSnykCliDefaultPath() : customPath;
            return File.Exists(path);
        }

        /// <summary>
        /// Try get Snyk API token from snyk cli config or throw <see cref="InvalidTokenException"/>.
        /// </summary>
        /// <returns>API token string.</returns>
        public string GetApiTokenOrThrowException()
        {
            string apiToken = this.ConsoleRunner.Run(this.GetCliPath(), "config get " + this.GetTokenKey());

            if (apiToken.IsNullOrEmpty())
            {
                throw new InvalidTokenException(string.IsNullOrEmpty(apiToken) ? string.Empty : apiToken);
            }

            return apiToken;
        }

        public void Authenticate()
        {
            var apiEndpointResolver = new ApiEndpointResolver(this.options);
            var environmentVariables = new StringDictionary();

            var args = new List<string> { "auth" };
            if (this.Options.IgnoreUnknownCA)
            {
                args.Add("--insecure");
            }

            if (apiEndpointResolver.AuthenticationMethod == AuthenticationType.OAuth)
            {
                args.Add("--auth-type=oauth");
                environmentVariables.Add("INTERNAL_SNYK_OAUTH_ENABLED", "1");

            }

            environmentVariables.Add(ApiEnvironmentVariableName, apiEndpointResolver.SnykApiEndpoint);

            var authResultMessage = this.ConsoleRunner.Run(this.GetCliPath(), string.Join(" ", args), environmentVariables);
            var authenticated = authResultMessage.Contains("Your account has been authenticated.");
            if (authenticated)
            {
                Logger.Information("Snyk auth executed successfully.");
            }
            else
            {
                var message = $"The `snyk auth` command failed to authenticate";
                throw new AuthenticationException(message);
            }
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

        /// <inheritdoc/>
        public string RunCommand(string arguments)
        {
            var cliPath = this.GetCliPath();

            Logger.Information("CLI path is {CliPath}", cliPath);

            var environmentVariables = new StringDictionary();
            var apiEndpointResolver = new ApiEndpointResolver(this.options);
            environmentVariables.Add(ApiEnvironmentVariableName, apiEndpointResolver.SnykApiEndpoint);

            this.ConsoleRunner.CreateProcess(cliPath, arguments, environmentVariables);

            Logger.Information("Start run console process");

            var consoleResult = this.ConsoleRunner.Execute();

            Logger.Information("Start convert console string result to CliResult and return value");

            return consoleResult;
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
            if (this.Options.ApiToken.IsValid())
            {
                var token = this.Options.ApiToken.ToString();
                var tokenEnvVar = "SNYK_TOKEN";

                if (this.Options.ApiToken.Type == AuthenticationType.OAuth)
                {
                    tokenEnvVar = "INTERNAL_OAUTH_TOKEN_STORAGE";
                    environmentVariables.Add("INTERNAL_SNYK_OAUTH_ENABLED", "1");
                }

                environmentVariables.Add(tokenEnvVar, token);
                Logger.Information("Token added from Options");
            }

            var apiEndpointResolver = new ApiEndpointResolver(this.options);
            environmentVariables.Add(ApiEnvironmentVariableName, apiEndpointResolver.SnykApiEndpoint);
            
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