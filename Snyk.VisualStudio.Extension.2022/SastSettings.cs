using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Snyk.Common;

namespace Snyk.VisualStudio.Extension
{
    /// <summary>
    /// Sast settings.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SastSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether SastEnabled or not on server.
        /// </summary>
        public bool SastEnabled { get; set; }

        /// <summary>
        /// Gets or sets local code engine settings.
        /// </summary>
        public LocalCodeEngine LocalCodeEngine { get; set; }

        /// <summary>
        /// Gets a value indicating whether Snyk Code enabled.
        /// Snyk Code enabled if SastEnabled = true and LocalCodeEngine.Enabled = false.
        /// </summary>
        public bool SnykCodeEnabled => this.SastEnabled;

        public string Org { get; set; }
        public IList<string> SupportedLanguages { get; set; }
        public bool ReportFalsePositivesEnabled { get; set; }
        public bool AutofixEnabled { get; set; }
        /// <summary>
        /// Gets a value indicating whether local code engine enabled/disabled.
        /// </summary>
        public bool LocalCodeEngineEnabled => this.LocalCodeEngine != null && this.LocalCodeEngine.Enabled;
    }
}
