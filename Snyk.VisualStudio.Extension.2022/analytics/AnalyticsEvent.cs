using System;
using System.Collections.Generic;

namespace Snyk.VisualStudio.Extension.Analytics
{
    public class AnalyticsEvent : IAbstractAnalyticsEvent
    {
        public string InteractionType { get; }
        public List<string> Category { get; }
        public string Status { get; }
        public string TargetId { get; }
        public long TimestampMs { get; }
        public long DurationMs { get; }
        public Dictionary<string, object> Results { get; }
        public List<object> Errors { get; }
        public Dictionary<string, object> Extension { get; }

        public AnalyticsEvent(
            string interactionType,
            List<string> category,
            string deviceId,
            string status = null,
            string targetId = null,
            long timestampMs = 0,
            long durationMs = 0,
            Dictionary<string, object> results = null,
            List<object> errors = null,
            Dictionary<string, object> extension = null)
        {
            InteractionType = interactionType;
            Category = category;
            Status = status ?? "success";
            TargetId = targetId ?? "pkg:filesystem/scrubbed";
            TimestampMs = timestampMs != 0 ? timestampMs : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            DurationMs = durationMs;
            Results = results ?? new Dictionary<string, object>();
            Errors = errors ?? [];
            Extension = extension ?? new Dictionary<string, object>();
            if (deviceId != null)
            {
                Extension["device_id"] = deviceId;
            }
        }

        public AnalyticsEvent(string interactionType, List<string> category, string deviceId)
            : this(interactionType, category, deviceId, null, null, 0, 0, null, null, null)
        {
        }
    }
}