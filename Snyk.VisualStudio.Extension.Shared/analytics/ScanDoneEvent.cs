using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Snyk.Common.Settings;

namespace Snyk.VisualStudio.Extension.Shared.CLI
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class UniqueIssueCount
    {
        public int Critical { get; set; }
        
        public int High { get; set; }
        
        public int Medium { get; set; }
        
        public int Low { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class Attributes
    {
        public Attributes(ISnykOptions options)
        {
            DeviceId = options.AnonymousId;
            Application = options.Application;
            ApplicationVersion = options.ApplicationVersion;
            IntegrationName = options.IntegrationName;
            IntegrationVersion = options.IntegrationVersion;
            IntegrationEnvironment = options.IntegrationEnvironment;
            IntegrationEnvironmentVersion = options.IntegrationEnvironmentVersion;
        }

        public string DeviceId { get;  }
        
        public string Application { get;  }
        
        public string ApplicationVersion { get;  }
        
        public string Os => "windows";
        
        public string Arch
        {
            get
            {
                return GetArch();

                string GetArch()
                {
                    var processArchitecture = RuntimeInformation.ProcessArchitecture;
                    return processArchitecture switch
                    {
                        Architecture.Arm64 => "arm64",
                        Architecture.X64 => "x86_64",
                        Architecture.X86 => "386",
                        _ => "Unknown",
                    };
                }
            }
        }

        public string IntegrationName { get;  }
        
        public string IntegrationVersion { get;  }
        
        public string IntegrationEnvironment { get;  }
        
        public string IntegrationEnvironmentVersion { get;  }
        
        public string EventType => "Scan done";
        
        public string Status => "Succeeded";
        
        public string ScanType { get; set; }
        
        public UniqueIssueCount UniqueIssueCount { get; set; }
        
        public string DurationMs { get; set; }

        public string TimestampFinished { get; } = DateTime.Now.ToUniversalTime()
            .ToString(DateTimeFormat, CultureInfo.InvariantCulture);

        [JsonIgnore]
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    }

    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class Data
    {
        public string Type { get; set; } = "analytics";
        
        public Attributes Attributes { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class ScanDoneEvent
    {
        public Data Data { get; set; }
    }
}