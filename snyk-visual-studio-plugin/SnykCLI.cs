using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using EnvDTE;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.CLI
{
    public class SnykCli
    {
        public const string CliFileName = "snyk-win.exe";
        public const string SnykConfigurationDirectoryName = "Snyk";

        private IServiceProvider serviceProvider;
        ISnykOptions options;
            
        public SnykCli() { }

        public SnykCli(ISnykOptions options, IServiceProvider serviceProvider)
        {
            Options = options;
            ServiceProvider = serviceProvider;            
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

        public CliResult ConvertRawCliStringToCliResult(String rawResultStr)
        {
            if (rawResultStr.First() == '[')
            {
                var cliVulnerabilitiesList = new List<CliVulnerabilities>();
                var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(rawResultStr));
                var jsonSerializer = new DataContractJsonSerializer(cliVulnerabilitiesList.GetType());

                cliVulnerabilitiesList = jsonSerializer.ReadObject(memoryStream) as List<CliVulnerabilities>;

                memoryStream.Close();                

                return new CliResult
                {
                    CLIVulnerabilities = cliVulnerabilitiesList
                };
            } else if (rawResultStr.First() == '{')
            {
                if (IsSuccessCliJsonString(rawResultStr))
                {
                    var cliVulnerabilities = new CliVulnerabilities();
                    var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(rawResultStr));
                    var jsonSerializer = new DataContractJsonSerializer(cliVulnerabilities.GetType());

                    cliVulnerabilities = jsonSerializer.ReadObject(memoryStream) as CliVulnerabilities;

                    memoryStream.Close();

                    var cliVulnerabilitiesList = new List<CliVulnerabilities>();
                    cliVulnerabilitiesList.Add(cliVulnerabilities);

                    return new CliResult
                    {
                        CLIVulnerabilities = cliVulnerabilitiesList
                    };
                } else
                {
                    var cliError = new CliError();
                    var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(rawResultStr));
                    var jsonSerializer = new DataContractJsonSerializer(cliError.GetType());

                    cliError = jsonSerializer.ReadObject(memoryStream) as CliError;

                    memoryStream.Close();
                    
                    return new CliResult
                    {
                        Error = cliError
                    };
                }
            } else
            {
                return new CliResult
                {
                    Error = new CliError
                    {
                        Message = rawResultStr
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
            DTE dte = (DTE) this.ServiceProvider.GetService(typeof(DTE));
            Projects projects = dte.Solution.Projects;

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

        public IServiceProvider ServiceProvider
        {
            get
            {
                return serviceProvider;
            }

            set
            {
                serviceProvider = value;
            }
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
