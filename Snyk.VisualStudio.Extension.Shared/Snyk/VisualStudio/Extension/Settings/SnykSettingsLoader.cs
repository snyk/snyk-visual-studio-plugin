namespace Snyk.VisualStudio.Extension.Settings
{
    using System;
    using System.IO;
    using System.Text;
    using Serilog;
    using Snyk.Common;

    /// <summary>
    /// Load and save Snyk settings.
    /// </summary>
    public class SnykSettingsLoader
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykSettingsLoader>();

        /// <summary>
        /// Load <see cref="SnykSettings"/> instance.
        /// </summary>
        /// <returns>SnykSettings object.</returns>
        public SnykSettings Load()
        {
            try
            {
                return Json.Deserialize<SnykSettings>(File.ReadAllText(this.GetSettingsFilePath(), Encoding.UTF8));
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
                File.WriteAllText(this.GetSettingsFilePath(), Json.Serialize(settings), Encoding.UTF8);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Settings serialize error on save.");
            }
        }

        private string GetSettingsFilePath() => Path.Combine(SnykExtension.GetExtensionDirectoryPath(), "settings.json");
    }
}