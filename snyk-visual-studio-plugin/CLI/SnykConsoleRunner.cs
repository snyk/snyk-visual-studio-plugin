using System;
using System.Diagnostics;
using System.Text;

namespace Snyk.VisualStudio.Extension.CLI
{
    public class SnykConsoleRunner
    {
        public virtual string RunConsoleProcess(string fileName, string arguments)
        {
            var consoleProcess = CreateConsoleProcess(fileName, arguments);

            return RunConsoleProcess(consoleProcess);
        }

        public virtual Process CreateConsoleProcess(string fileName, string arguments)
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

        public virtual string RunConsoleProcess(Process consoleProcess)
        {
            var stringBuilder = new StringBuilder();

            try
            {
                consoleProcess.Start();

                while (!consoleProcess.StandardOutput.EndOfStream)
                {
                    stringBuilder.AppendLine(consoleProcess.StandardOutput.ReadLine());
                }
            }
            catch (Exception exception)
            {
                stringBuilder.Append(exception.Message);
            }

            return stringBuilder.ToString().Replace("\n", "").Replace("\r", "");
        }
    }
}
