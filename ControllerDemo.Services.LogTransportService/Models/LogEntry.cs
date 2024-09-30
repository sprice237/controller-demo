using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;

namespace ControllerDemo.Services.LogTransportService.Models
{
    public class LogEntry
    {
        [JsonProperty("pid")] public int Pid { get; set; }
        [JsonProperty("uid")] public int Uid { get; set; }
        [JsonProperty("gid")] public int Gid { get; set; }
        [JsonProperty("systemdUnit")] public string SystemdUnit { get; set; }
        [JsonProperty("timestamp")] public DateTimeOffset Timestamp { get; set; }
        [JsonProperty("journalCursor")] public string JournalCursor { get; set; }
        [JsonProperty("logLevel")] public string LogLevel { get; set; }
        [JsonProperty("message")] public string Message { get; set; }

        public LogEntry() { }

        public LogEntry(JournalLogEntry journalLogEntry)
        {
            Pid = int.Parse(journalLogEntry.Pid);
            Uid = int.Parse(journalLogEntry.Uid);
            Gid = int.Parse(journalLogEntry.Gid);

            SystemdUnit = journalLogEntry.SystemdUnit;
            Timestamp = journalLogEntry.Timestamp;
            JournalCursor = journalLogEntry.Cursor;

            switch (journalLogEntry.Priority)
            {
                case JournalLogEntryPriority.EMERG:
                case JournalLogEntryPriority.ALERT:
                case JournalLogEntryPriority.CRIT:
                    LogLevel = "FATAL";
                    break;
                case JournalLogEntryPriority.ERR:
                    LogLevel = "ERROR";
                    break;
                case JournalLogEntryPriority.WARNING:
                case JournalLogEntryPriority.NOTICE:
                    LogLevel = "WARN";
                    break;
                case JournalLogEntryPriority.INFO:
                    LogLevel = "INFO";
                    break;
                case JournalLogEntryPriority.DEBUG:
                    LogLevel = "DEBUG";
                    break;
            }

            Message = journalLogEntry.Message.ValueKind switch
            {
                JsonValueKind.String => journalLogEntry.Message.GetString(),
                JsonValueKind.Array => string.Join("\n", journalLogEntry.Message.EnumerateArray().Select(line => line.GetString())),
                _ => throw new InvalidCastException("Message is not a string or a list of strings")
            };
        }
    }
}