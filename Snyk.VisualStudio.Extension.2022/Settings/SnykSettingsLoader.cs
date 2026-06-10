using System;
using System.IO;
using System.Text;
using Serilog;
using Snyk.VisualStudio.Extension.Utils;

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

                var rawJson = File.ReadAllText(this.settingsFilePath, Encoding.UTF8);

                // Visibility for support: the legacy per-solution store (solutionSettingsDict) was
                // removed in IDE-1651. Folder-scoped settings are now owned/persisted by snyk-ls and
                // re-sent via $/snyk.configuration, so no migration is performed — but warn if an
                // upgrading user's settings.json still carries a non-empty legacy section, since
                // those IDE-local values will be dropped on the next save.
                WarnIfLegacySolutionSettingsPresent(rawJson);

                snykSettings = Json.Deserialize<SnykSettings>(rawJson);
                return snykSettings;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Settings deserialize error on load.");

                return null;
            }
        }

        private static void WarnIfLegacySolutionSettingsPresent(string rawJson)
        {
            try
            {
                var token = Newtonsoft.Json.Linq.JObject.Parse(rawJson)["solutionSettingsDict"]
                    as Newtonsoft.Json.Linq.JObject;
                if (token != null && token.Count > 0)
                {
                    Logger.Warning(
                        "settings.json contains a legacy 'solutionSettingsDict' with {Count} entr(ies). " +
                        "Per-solution settings are now managed by the Language Server; these legacy IDE-local " +
                        "values are ignored and will be removed on the next save.",
                        token.Count);
                }
            }
            catch (Exception e)
            {
                // Best-effort diagnostics only — never block load on a malformed legacy section.
                Logger.Debug(e, "Could not inspect settings.json for a legacy solutionSettingsDict section.");
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