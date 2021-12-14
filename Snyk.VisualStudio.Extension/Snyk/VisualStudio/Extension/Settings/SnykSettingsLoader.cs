namespace Snyk.VisualStudio.Extension.Settings
{
    using System;
    using System.IO;
    using System.Text;
    using Snyk.Common;

    /// <summary>
    /// Load and save Snyk settings.
    /// </summary>
    public class SnykSettingsLoader
    {
        /// <summary>
        /// Gets or sets logger.
        /// </summary>
        public SnykActivityLogger Logger { get; set; }

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
            catch (Exception exception)
            {
                this.Logger.LogError(exception.Message);

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
            catch (Exception exception)
            {
                this.Logger.LogError(exception.Message);
            }
        }

        private string GetSettingsFilePath() => Path.Combine(SnykExtension.GetExtensionDirectoryPath(), "settings.json");
    }
}
