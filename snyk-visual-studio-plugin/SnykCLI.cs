using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.Util;

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

        public SnykCli(IServiceProvider serviceProvider): base()
        {
            Options = options;            
        }

        public SnykConsoleRunner ConsoleRunner { get; set; }

        public string GetApiToken() => ConsoleRunner.RunConsoleProcess(GetSnykCliPath(), "config get api");

        public string Authenticate() => ConsoleRunner.RunConsoleProcess(GetSnykCliPath(), "auth");

        public CliResult Scan(string basePath)
        {
            var consoleProcess = ConsoleRunner.CreateConsoleProcess(GetSnykCliPath(), BuildArguments());

            if (!String.IsNullOrEmpty(Options.ApiToken))
            {
                consoleProcess.StartInfo.EnvironmentVariables["SNYK_TOKEN"] = Options.ApiToken;
            }

            consoleProcess.StartInfo.WorkingDirectory = basePath;

            string consoleResult = ConsoleRunner.RunConsoleProcess(consoleProcess);

            return ConvertRawCliStringToCliResult(consoleResult);
        }

        public string BuildArguments()
        {
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

            return String.Join(" ", arguments.ToArray());
        }

        public CliResult ConvertRawCliStringToCliResult(String rawResult)
        {
            if (rawResult.First() == '[')
            {
                return new CliResult
                {
                    CLIVulnerabilities = Json.Deserialize(rawResult, typeof(List<CliVulnerabilities>)) as List<CliVulnerabilities>
                };
            } else if (rawResult.First() == '{')
            {
                if (IsSuccessCliJsonString(rawResult))
                {
                    var cliVulnerabilities = Json.Deserialize(rawResult, typeof(CliVulnerabilities)) as CliVulnerabilities;
                    
                    var cliVulnerabilitiesList = new List<CliVulnerabilities>();
                    cliVulnerabilitiesList.Add(cliVulnerabilities);

                    return new CliResult
                    {
                        CLIVulnerabilities = cliVulnerabilitiesList
                    };
                } else
                {
                    return new CliResult
                    {
                        Error = Json.Deserialize(rawResult, typeof(CliError)) as CliError
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
