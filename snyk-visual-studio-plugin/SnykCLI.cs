using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.Util;
using System.Text.RegularExpressions;

namespace Snyk.VisualStudio.Extension.CLI
{
    public class SnykCli
    {
        public const string CliFileName = "snyk-win.exe";
        public const string SnykConfigurationDirectoryName = "Snyk";

        private ISnykOptions options;        

        public SnykCli() { }

        public SnykCli(IServiceProvider serviceProvider)
        {
            Options = options;
        }

        public string GetApiToken()
        {
            var consoleProcess = CreateConsoleProcess(GetSnykCliPath(), "config get api");
            
            return RunConsoleProcess(consoleProcess);
        }

        public void Authenticate()
        {            
            var consoleProcess = CreateConsoleProcess(GetSnykCliPath(), "auth");

            RunConsoleProcess(consoleProcess);            
        }

        public CliResult Scan(string projectPath)
        {
            var consoleProcess = CreateConsoleProcess(GetSnykCliPath(), BuildArguments());

            if (!String.IsNullOrEmpty(Options.ApiToken))
            {
                consoleProcess.StartInfo.EnvironmentVariables["SNYK_TOKEN"] = Options.ApiToken;
            }

            consoleProcess.StartInfo.WorkingDirectory = projectPath;

            string consoleResult = RunConsoleProcess(consoleProcess);

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

        public static string GetSnykCliPath()
        {
            return Path.Combine(GetSnykDirectoryPath(), CliFileName);
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

        private Process CreateConsoleProcess(string fileName, string arguments)
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };            
        }       

        private string RunConsoleProcess(Process consoleProcess)
        {           
            var stringBuilder = new StringBuilder();

            try
            {
                consoleProcess.Start();

                while (!consoleProcess.StandardOutput.EndOfStream)
                {
                    stringBuilder.AppendLine(consoleProcess.StandardOutput.ReadLine());
                }
            } catch (Exception exception)
            {
                stringBuilder.Append(exception.Message);
            }

            return stringBuilder.ToString().Replace("\n", "").Replace("\r", "");
        }
    }   
}
