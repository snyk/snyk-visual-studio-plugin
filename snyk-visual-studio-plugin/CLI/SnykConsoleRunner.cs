using System;
using System.Diagnostics;
using System.Text;

namespace Snyk.VisualStudio.Extension.CLI
{
    public class SnykConsoleRunner
    {
        private bool isStopped = false;

        public Process Process { get; set; }

        public virtual string Run(string fileName, string arguments)
        {
            CreateProcess(fileName, arguments);

            return Execute();
        }

        public virtual Process CreateProcess(string fileName, string arguments)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            processStartInfo.EnvironmentVariables["SNYK_INTEGRATION_NAME"] = SnykExtension.IntegrationName;
            processStartInfo.EnvironmentVariables["SNYK_INTEGRATION_VERSION"] = SnykExtension.GetIntegrationVersion();

            Process = new Process
            {
                StartInfo = processStartInfo
            };

            return Process;
        }

        public virtual string Execute()
        {
            var stringBuilder = new StringBuilder();

            try
            {
                Process.Start();

                while (!Process.StandardOutput.EndOfStream)
                {
                    stringBuilder.AppendLine(Process.StandardOutput.ReadLine());
                }
            }
            catch (Exception exception)
            {
                stringBuilder.Append(exception.Message);
            }

            Process = null;

            return stringBuilder.ToString().Replace("\n", "").Replace("\r", "");
        }

        public void Stop()
        {
            Process?.Kill();

            this.isStopped = true;
        }

        public bool IsStopped
        {
            get
            {
                return isStopped;
            }
        }
    }
}
