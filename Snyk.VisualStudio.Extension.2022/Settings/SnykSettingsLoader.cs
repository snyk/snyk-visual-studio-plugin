using System;
using System.IO;
using System.Text;
using Serilog;
using Snyk.Common;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Load and save Snyk settings.
    /// </summary>
    public class SnykSettingsLoader
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykSettingsLoader>();

        private readonly string settingsFilePath;

        private SnykSettings snykSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykSettingsLoader"/> class.
        /// </summary>
        /// <param name="settingsPath">File path to settings.</param>
        public SnykSettingsLoader(string settingsPath)
        {
            this.settingsFilePath = settingsPath;
        }

        /// <summary>
        /// Load <see cref="SnykSettings"/> instance.
        /// </summary>
        /// <returns>SnykSettings object.</returns>
        public SnykSettings Load()
        {
            if (snykSettings != null)
                return snykSettings;

            try
            {
                if (!File.Exists(this.settingsFilePath))
                {
                    return null;
                }

                snykSettings = Json.Deserialize<SnykSettings>(File.ReadAllText(this.settingsFilePath, Encoding.UTF8));
                return snykSettings;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Settings deserialize error on load.");

                return null;
            }
        }

        /// <summary>
        /// Save <see cref="SnykSettings"/> to file.
        /// </summary>
        /// <param name="settings">Updated settings.</param>
        public void Save(SnykSettings settings)
        {
            try
            {
                File.WriteAllText(this.settingsFilePath, Json.Serialize(settings), Encoding.UTF8);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Settings serialize error on save.");
            }
        }
    }
}