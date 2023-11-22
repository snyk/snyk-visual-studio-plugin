using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Snyk.Common.Settings;

namespace Snyk.VisualStudio.Extension.Shared.CLI
{
    public class UniqueIssueCount
    {
        [JsonPropertyName("critical")]
        public int Critical { get; set; }
        
        [JsonPropertyName("high")]
        public int High { get; set; }
        
        [JsonPropertyName("medium")]
        public int Medium { get; set; }
        
        [JsonPropertyName("low")]
        public int Low { get; set; }
    }

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

        [JsonPropertyName("device_id")]
        public string DeviceId { get;  }
        
        [JsonPropertyName("application")]
        public string Application { get;  }
        
        [JsonPropertyName("application_version")]
        public string ApplicationVersion { get;  }
        
        [JsonPropertyName("os")]
        public string Os => "windows";
        
        [JsonPropertyName("arch")]
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

        [JsonPropertyName("integration_name")]
        public string IntegrationName { get;  }
        
        [JsonPropertyName("integration_version")]
        public string IntegrationVersion { get;  }
        
        [JsonPropertyName("integration_environment")]
        public string IntegrationEnvironment { get;  }
        
        [JsonPropertyName("integration_environment_version")]
        public string IntegrationEnvironmentVersion { get;  }
        
        [JsonPropertyName("event_type")]
        public string EventType => "Scan done";
        
        [JsonPropertyName("status")]
        public string Status => "Succeeded";
        
        [JsonPropertyName("scan_type")]
        public string ScanType { get; set; }
        
        [JsonPropertyName("unique_issue_count")]
        public UniqueIssueCount UniqueIssueCount { get; set; }
        
        [JsonPropertyName("duration_ms")]
        public string DurationMs { get; set; }

        [JsonPropertyName("timestamp_finished")]
        public string TimestampFinished { get; } = DateTime.Now.ToUniversalTime()
            .ToString(DateTimeFormat, CultureInfo.InvariantCulture);

        [JsonIgnore]
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    }

    public class Data
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "analytics";
        
        [JsonPropertyName("attributes")]
        public Attributes Attributes { get; set; }
    }

    public class ScanDoneEvent
    {
        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }
}