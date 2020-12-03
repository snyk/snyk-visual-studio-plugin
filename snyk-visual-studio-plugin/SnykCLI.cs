using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using EnvDTE;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.Services;
using Snyk.VisualStudio.Extension.Util;

namespace Snyk.VisualStudio.Extension.CLI
{
    public class SnykCli
    {
        public const string CliFileName = "snyk-win.exe";
        public const string SnykConfigurationDirectoryName = "Snyk";

        private ISnykOptions options;
        private SnykSolutionService solutionService;

        public SnykCli() { }

        public SnykCli(IServiceProvider serviceProvider, SnykSolutionService solutionService)
        {
            Options = options;
            SolutionService = solutionService;            
        }

        public CliResult Scan()
        {           
            var cliProcess = new System.Diagnostics.Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GetSnykCliPath(),
                    Arguments = BuildArguments(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };

            cliProcess.StartInfo.EnvironmentVariables["SNYK_TOKEN"] = Options.ApiToken;
            cliProcess.StartInfo.WorkingDirectory = GetProjectDirectory();

            StringBuilder stringBuilder = new StringBuilder();

            cliProcess.Start();

            while (!cliProcess.StandardOutput.EndOfStream)
            {
                stringBuilder.AppendLine(cliProcess.StandardOutput.ReadLine());
            }
            
            return ConvertRawCliStringToCliResult(stringBuilder.ToString());
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
                var cliVulnerabilitiesList = Json.Deserialize(rawResult, typeof(List<CliVulnerabilities>)) as List<CliVulnerabilities>;
                
                return new CliResult
                {
                    CLIVulnerabilities = cliVulnerabilitiesList
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

        public bool IsSuccessCliJsonString(string JsonStr)
        {
            return JsonStr.Contains("\"vulnerabilities\":") && !JsonStr.Contains("\"error\":");
        }

        public string GetProjectDirectory()
        {
            Projects projects = SolutionService.GetProjects();

            if (projects.Count == 0)
            {
                throw new ArgumentException("No open projects.");
            }

            Project project = projects.Item(1);

            return project.Properties.Item("LocalPath").Value.ToString();            
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

        public SnykSolutionService SolutionService
        {
            get
            {
                return solutionService;
            }

            set
            {
                solutionService = value;
            }
        }
    }   
}
