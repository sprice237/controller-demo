using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ControllerDemo.Services.LogTransportService.Models
{
    public enum JournalLogEntryPriority
    {
        EMERG = 0,
        ALERT = 1,
        CRIT = 2,
        ERR = 3,
        WARNING = 4,
        NOTICE = 5,
        INFO = 6,
        DEBUG = 7
    }

    public class JournalLogEntry
    {
        [JsonIgnore] public string OriginalLine { get; set; }
        [JsonPropertyName("_PID")] public string Pid { get; set; }
        [JsonPropertyName("_UID")] public string Uid { get; set; }
        [JsonPropertyName("_GID")] public string Gid { get; set; }
        [JsonPropertyName("_SYSTEMD_UNIT")] public string SystemdUnit { get; set; }
        [JsonPropertyName("__REALTIME_TIMESTAMP")] public string EpochTimeMicroseconds { get; set; }
        [JsonPropertyName("__CURSOR")] public string Cursor { get; set; }
        [JsonPropertyName("PRIORITY")] public string PriorityStr { get; set; }
        [JsonPropertyName("MESSAGE")] public JsonElement Message { get; set; }

        [JsonIgnore]
        public DateTimeOffset Timestamp => DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(EpochTimeMicroseconds) / 1000);

        [JsonIgnore]
        public JournalLogEntryPriority? Priority => PriorityStr == null ? (JournalLogEntryPriority?)null : (JournalLogEntryPriority)int.Parse(PriorityStr);
    }
}