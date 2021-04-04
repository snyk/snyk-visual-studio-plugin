using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.CLI
{
    public class SnykCli
    {
        public const string CliFileName = "snyk-win.exe";
        public const string SnykConfigurationDirectoryName = "Snyk";

        private ISnykOptions options;        

        public SnykCli()
        {
            ConsoleRunner = new SnykConsoleRunner();
        }

        public SnykConsoleRunner ConsoleRunner { get; set; }

        public string GetApiToken() => ConsoleRunner.Run(GetSnykCliPath(), "config get api");

        public string Authenticate() => ConsoleRunner.Run(GetSnykCliPath(), "auth");

        public CliResult Scan(string basePath)
        {
            Logger?.LogInformation("Enter Scan() method");
            Logger?.LogInformation($"Base path is {basePath}");

            string cliPath = GetSnykCliPath();

            Logger?.LogInformation($"CLI path is {cliPath}");

            ConsoleRunner.CreateProcess(cliPath, BuildArguments());

            Logger?.LogInformation("Adding token");

            if (!String.IsNullOrEmpty(Options.ApiToken))
            {
                Logger?.LogInformation("Token added from Options");

                ConsoleRunner.Process.StartInfo.EnvironmentVariables["SNYK_TOKEN"] = Options.ApiToken;
            }            

            ConsoleRunner.Process.StartInfo.WorkingDirectory = basePath;
            
            Logger?.LogInformation("Start run console process");

            string consoleResult = ConsoleRunner.Execute();

            Logger?.LogInformation("Leave Scan() method");

            return ConvertRawCliStringToCliResult(consoleResult);
        }

        public string BuildArguments()
        {
            Logger?.LogInformation("Enter BuildArguments method");

            var arguments = new List<string>();

            arguments.Add("--json");
            arguments.Add("test");

            if (!String.IsNullOrEmpty(Options.CustomEndpoint))
            {
                arguments.Add($"--api={Options.CustomEndpoint}");
            }

            if (Options.IgnoreUnknownCA)
            {
                arguments.Add("--insecure");
            }

            if (!String.IsNullOrEmpty(Options.Organization))
            {
                arguments.Add($"--org={Options.Organization}");
            }

            if (!String.IsNullOrEmpty(Options.AdditionalOptions))
            {
                arguments.Add($"{Options.AdditionalOptions}");
            }

            if (Options.IsScanAllProjects) 
            {
                arguments.Add("--all-projects");
            }

            if (!Options.UsageAnalyticsEnabled)
            {
                arguments.Add("--DISABLE_ANALYTICS");
            }

            string cliArguments = String.Join(" ", arguments.ToArray());

            Logger?.LogInformation($"Result CLI arguments {cliArguments}");
            Logger?.LogInformation("L:eave BuildArguments method");

            return cliArguments;
        }

        public CliResult ConvertRawCliStringToCliResult(String rawResult)
        {
            if (rawResult.First() == '[')
            {
                return new CliResult
                {
                    CliVulnerabilitiesList = JsonConvert.DeserializeObject<List<CliVulnerabilities>>(rawResult)                
                };
            } else if (rawResult.First() == '{')
            {
                if (IsSuccessCliJsonString(rawResult))
                {
                    var cliVulnerabilities = JsonConvert.DeserializeObject<CliVulnerabilities>(rawResult);

                    var cliVulnerabilitiesList = new List<CliVulnerabilities>();
                    cliVulnerabilitiesList.Add(cliVulnerabilities);

                    return new CliResult
                    {
                        CliVulnerabilitiesList = cliVulnerabilitiesList
                    };
                } else
                {
                    return new CliResult
                    {
                        Error = JsonConvert.DeserializeObject<CliError>(rawResult)
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
                        Path = ""
                    }
                };
            }
        }               

        public bool IsSuccessCliJsonString(string json)
        {
            return json.Contains("\"vulnerabilities\":") && !json.Contains("\"error\":");
        }

        public static string GetSnykDirectoryPath()
        {
            string appDataDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return Path.Combine(appDataDirectoryPath, SnykConfigurationDirectoryName);
        }

        public static string GetSnykCliPath() => Path.Combine(GetSnykDirectoryPath(), CliFileName);
        
        public static bool IsCliExists() => File.Exists(GetSnykCliPath());

        public SnykActivityLogger Logger
        {
            get; set;
        }

        public ISnykOptions Options
        {
            get
            {
                return options;
            }

            set
            {
                options = value;
            }
        }                      
    }   
}
