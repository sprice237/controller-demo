using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ControllerDemo.Rabbit;
using ControllerDemo.Services.LogTransportService.Helpers;
using ControllerDemo.Services.LogTransportService.MessageTypes;
using ControllerDemo.Services.LogTransportService.Models;

namespace ControllerDemo.Services.LogTransportService.Loops
{
    public class JournalLogsLoop
    {
        private readonly ILogger<JournalLogsLoop> _logger;
        private readonly OfflineCachedLogsHelper _offlineCachedLogsHelper;
        private readonly RabbitConnectionManager _rabbitConnectionManager;

        public JournalLogsLoop(ILogger<JournalLogsLoop> logger, OfflineCachedLogsHelper offlineCachedLogsHelper, RabbitConnectionManager rabbitConnectionManager)
        {
            _logger = logger;
            _offlineCachedLogsHelper = offlineCachedLogsHelper;
            _rabbitConnectionManager = rabbitConnectionManager;
        }


        public async void StartLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var journalLogEntries = await GetJournalLogs();
                    _logger.LogTrace($"Obtained {journalLogEntries.Count} log entries from systemd journal");

                    _logger.LogTrace($"Sending log entries to rabbit");
                    _rabbitConnectionManager.RabbitHelper.QueueDeclare<LogEntryMessage>();
                    foreach (var journalLogEntry in journalLogEntries)
                    {
                        _rabbitConnectionManager.RabbitHelper.SendMessage(new LogEntryMessage
                        {
                            JournalLogEntry = journalLogEntry
                        });
                    }
                    _logger.LogTrace($"Finished sending log entries to rabbit");

                    var newLastCursor = journalLogEntries.LastOrDefault()?.Cursor;
                    if (newLastCursor != null)
                    {
                        await _offlineCachedLogsHelper.WriteLastCursor(newLastCursor);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred in JournalLogsHelper");
                }
                finally
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        private async Task<List<JournalLogEntry>> GetJournalLogs()
        {
            var lastCursor = await _offlineCachedLogsHelper.ReadLastCursor();

            try
            {
                var lines = ReadJournal(lastCursor);

                var logs = lines
                    .Select(line =>
                    {
                        var journalLogEntry = JsonSerializer.Deserialize<JournalLogEntry>(line);
                        journalLogEntry.OriginalLine = line;
                        return journalLogEntry;
                    })
                    .ToList();

                return logs;
            }
            catch (Exception e)
            {
                var lastCursorForLog = lastCursor != null ? $"\"{lastCursor}\"" : "null";
                _logger.LogError(e, $"An error occurred reading from the systemd journal (lastCursor == {lastCursorForLog}");
                _offlineCachedLogsHelper.DeleteLastCursor();

                return new List<JournalLogEntry>();
            }
        }

        private List<string> ReadJournal(string lastCursor)
        {
            using var journalctlProcess = new Process
            {
                StartInfo =
                {
                    FileName = "journalctl", RedirectStandardOutput = true, RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            journalctlProcess.StartInfo.ArgumentList.Add("--output=json");
            journalctlProcess.StartInfo.ArgumentList.Add("--all");
            journalctlProcess.StartInfo.ArgumentList.Add("--utc");

            if (!string.IsNullOrEmpty(lastCursor))
            {
                journalctlProcess.StartInfo.ArgumentList.Add($"--after-cursor={lastCursor}");
            }

            journalctlProcess.Start();


            var lines = new List<string>();
            journalctlProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    lines.Add(e.Data);
                }
            };

            var errors = new List<string>();
            journalctlProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errors.Add(e.Data);
                }
            };

            journalctlProcess.BeginOutputReadLine();
            journalctlProcess.BeginErrorReadLine();
            journalctlProcess.WaitForExit();

            if (errors.Any())
            {
                if (errors.Count == 1)
                {
                    throw new Exception(errors[0]);
                }
                else
                {
                    throw new AggregateException(errors.Select(e => new Exception(e)));
                }
            }

            return lines;
        }
    }
}