using Snyk.Common.Settings;

namespace Snyk.VisualStudio.Extension.CLI
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Text;
    using Serilog;
    using Snyk.Common;

    /// <summary>
    /// Incapsulate work with console/terminal.
    /// </summary>
    public class SnykConsoleRunner
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykConsoleRunner>();

        private bool isStopped = false;
        private readonly string ideVersion;
        private readonly ISnykOptions options;

        public SnykConsoleRunner(ISnykOptions options, string ideVersion = "")
        {
            this.ideVersion = ideVersion;
            this.options = options;
        }

        /// <summary>
        /// Gets or sets a value indicating whether process.
        /// </summary>
        private Process Process { get; set; }

        /// <summary>
        /// Gets a value indicating whether is current process is stoped or still running.
        /// </summary>
        public bool IsStopped => this.isStopped;

        /// <summary>
        /// Run file name with arguments.
        /// </summary>
        /// <param name="fileName">Path to file for run.</param>
        /// <param name="arguments">Arguments for programm to run.</param>
        /// <param name="environmentVariables">Environment variables to set for a process.</param>
        /// <returns>Result string from programm.</returns>
        public virtual string Run(string fileName, string arguments, StringDictionary environmentVariables = null)
        {
            this.CreateProcess(fileName, arguments, environmentVariables);

            return this.Execute();
        }

        /// <summary>
        /// Create process to run external programm in console.
        /// </summary>
        /// <param name="fileName">Programm file name (full path).</param>
        /// <param name="arguments">Arguments for programm to run.</param>
        /// <param name="environmentVariables">Environment variables to set for a process.</param>
        /// <param name="workingDirectory">Working directory to set for a process.</param>
        /// <returns>Result process.</returns>
        public virtual Process CreateProcess(string fileName, string arguments, StringDictionary environmentVariables = null, string workingDirectory = null)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            if (environmentVariables != null)
            {
                foreach (DictionaryEntry keyValuePair in environmentVariables)
                {
                    processStartInfo.EnvironmentVariables[keyValuePair.Key.ToString().ToUpper()] = keyValuePair.Value.ToString();
                }
            }

            if (!this.options.ErrorReportsEnabled)
            {
                processStartInfo.EnvironmentVariables["SNYK_CFG_DISABLE_ANALYTICS"] = "1";
            }
            
            processStartInfo.EnvironmentVariables["SNYK_INTEGRATION_NAME"] = SnykExtension.IntegrationName;
            processStartInfo.EnvironmentVariables["SNYK_INTEGRATION_VERSION"] = SnykExtension.Version;
            processStartInfo.EnvironmentVariables["SNYK_INTEGRATION_ENVIRONMENT_NAME"] = SnykExtension.IntegrationName;
            processStartInfo.EnvironmentVariables["SNYK_INTEGRATION_ENVIRONMENT_VERSION"] = this.ideVersion;

            processStartInfo.WorkingDirectory = workingDirectory;

            this.Process = new Process
            {
                StartInfo = processStartInfo,
            };

            return this.Process;
        }

        /// <summary>
        /// Execute current process.
        /// </summary>
        /// <returns>Return result from external process.</returns>
        public virtual string Execute()
        {
            var stringBuilder = new StringBuilder();

            try
            {
                this.Process.Start();

                while (!this.Process.StandardOutput.EndOfStream)
                {
                    stringBuilder.AppendLine(this.Process.StandardOutput.ReadLine());
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Cli running process error.");

                stringBuilder.Append(exception.Message);
            }

            this.Process = null;

            return stringBuilder.ToString().Replace("\n", string.Empty).Replace("\r", string.Empty);
        }

        /// <summary>
        /// Stop (kill) current running process.
        /// </summary>
        public void Stop()
        {
            try
            {
                this.Process?.Kill();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Cli running process error.");
            }

            this.isStopped = true;
        }
    }
}