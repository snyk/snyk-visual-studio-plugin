using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using EnvDTE;

namespace snyk_visual_studio_plugin
{
    class SnykCLI
    {
        public const string CliFileName = "snyk-win.exe";

        private IServiceProvider ServiceProvider;
        private SnykVSPackage package;

        public SnykCLI(SnykVSPackage snykPackage, IServiceProvider ServiceProvider)
        {
            this.ServiceProvider = ServiceProvider;
            this.package = snykPackage;
        }

        public CLIResult Scan()
        {
            StringBuilder commandsStringBuilder = new StringBuilder("--json test ");

            if (!String.IsNullOrEmpty(package.CustomEndpoint))
            {
                commandsStringBuilder.Append(String.Format(" --api=%s ", package.CustomEndpoint));
            }

            if (package.IgnoreUnknownCA)
            {
                commandsStringBuilder.Append(" --insecure ");
            }

            if (!String.IsNullOrEmpty(package.Organization))
            {                
                commandsStringBuilder.Append(String.Format(" --org=%s ", package.Organization));
            }

            if (!String.IsNullOrEmpty(package.AdditionalOptions))
            {
                commandsStringBuilder.Append(String.Format(" %s ", package.AdditionalOptions));
            }

            var cliProcess = new System.Diagnostics.Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GetSnykCliPath(),
                    Arguments = commandsStringBuilder.ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };

            cliProcess.StartInfo.EnvironmentVariables["SNYK_TOKEN"] = package.ApiToken;
            cliProcess.StartInfo.WorkingDirectory = GetProjectDirectory();

            StringBuilder stringBuilder = new StringBuilder();

            cliProcess.Start();

            while (!cliProcess.StandardOutput.EndOfStream)
            {
                stringBuilder.AppendLine(cliProcess.StandardOutput.ReadLine());
            }
            
            return ConvertRawCliStringToCliResult(stringBuilder.ToString());
        }

        public CLIResult ConvertRawCliStringToCliResult(String rawResultStr)
        {
            if (rawResultStr.First() == '[')
            {
                // TODO convert to CLIResult
                var cliVulnerabilitiesList = new List<CLIVulnerabilities>();
                var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(rawResultStr));
                var jsonSerializer = new DataContractJsonSerializer(cliVulnerabilitiesList.GetType());

                cliVulnerabilitiesList = jsonSerializer.ReadObject(memoryStream) as List<CLIVulnerabilities>;

                memoryStream.Close();                

                return new CLIResult
                {
                    CLIVulnerabilities = cliVulnerabilitiesList
                };
            } else if (rawResultStr.First() == '{')
            {
                if (IsSuccessCliJsonString(rawResultStr))
                {
                    // TODO convert to CLIResult
                    var cliVulnerabilities = new CLIVulnerabilities();
                    var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(rawResultStr));
                    var jsonSerializer = new DataContractJsonSerializer(cliVulnerabilities.GetType());

                    cliVulnerabilities = jsonSerializer.ReadObject(memoryStream) as CLIVulnerabilities;

                    memoryStream.Close();

                    var cliVulnerabilitiesList = new List<CLIVulnerabilities>();
                    cliVulnerabilitiesList.Add(cliVulnerabilities);

                    return new CLIResult
                    {
                        CLIVulnerabilities = cliVulnerabilitiesList
                    };
                } else
                {
                    // TODO convert to CLIError and return CLIResult with error

                    // TODO convert to CLIResult
                    var cliError = new CLIError();
                    var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(rawResultStr));
                    var jsonSerializer = new DataContractJsonSerializer(cliError.GetType());

                    cliError = jsonSerializer.ReadObject(memoryStream) as CLIError;

                    memoryStream.Close();
                    
                    return new CLIResult
                    {
                        Error = cliError
                    };
                }
            } else
            {
                // TODO CLIResult with CLIError. CLIError create and add raw result string.
                return new CLIResult
                {
                    Error = new CLIError
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

            if (projects.Count == 0)   // no project is open
            {
                Console.WriteLine("Process case if no projects.");
            }

            Project project = projects.Item(1);

            return project.Properties.Item("LocalPath").Value.ToString();            
        }

        public static string GetSnykDirectoryPath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return appDataPath + Path.DirectorySeparatorChar + "Snyk";
        }

        public static string GetSnykCliPath()
        {
            return GetSnykDirectoryPath() + Path.DirectorySeparatorChar + CliFileName;
        }
    }   
}
