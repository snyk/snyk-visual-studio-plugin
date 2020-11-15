using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace snyk_visual_studio_plugin
{
    class SnykCLI
    {
        public const string CliFileName = "snyk-win.exe";

        public void Scan()
        {
            var cliProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GetSnykCliPath(),
                    Arguments = "--json test",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            StringBuilder stringBuilder = new StringBuilder();

            cliProcess.Start();

            while (!cliProcess.StandardOutput.EndOfStream)
            {
                stringBuilder.AppendLine(cliProcess.StandardOutput.ReadLine());
            }

            Console.WriteLine(stringBuilder.ToString());
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
