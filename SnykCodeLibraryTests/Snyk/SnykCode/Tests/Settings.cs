namespace Snyk.SnykCode.Tests
{    
    using System;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    /// Test settings.
    /// </summary>
    public class Settings
    {
        private const string SettingsFileName = "settings.json";

        /// <summary>
        /// Gets or sets a value indicating whether Api token.
        /// </summary>
        public string ApiToken { get; set; }

        private static Settings instance;

        /// <summary>
        /// Gets a value indicating whether Settings instance. If settings not loaded it will load it first from settings.json.
        /// </summary>
        /// <returns><see cref="Settings"/> instance.</returns>
        public static Settings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Load();
                }

                return instance;
            }
        }

        private static Settings Load()
        {
            string settingsPath = Path.Combine(GetProjectPath(), SettingsFileName);

            return JsonConvert
                .DeserializeObject<Settings>(File.ReadAllText(settingsPath, Encoding.UTF8));
        }

        private static string GetProjectPath()
        {
            string workingDirectory = Environment.CurrentDirectory;

            string projectDirectory = Directory.GetParent(workingDirectory).Parent.FullName;

            return Directory.GetParent(workingDirectory).Parent.Parent.FullName;
        }
    }
}
