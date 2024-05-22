using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.Shared.Language
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class LspAnalysisResult
    {
        public string Status { get; set; }
        public string Product { get; set; }
        public string FolderPath { get; set; }

    }
}
