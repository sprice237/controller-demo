using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ControllerDemo.Common;
using ControllerDemo.Rabbit;
using ControllerDemo.Services.LogTransportService.MessageTypes;
using ControllerDemo.Services.LogTransportService.Models;
using SocketIOClient;

namespace ControllerDemo.Services.LogTransportService.Loops
{
    public class ShipLogsLoop
    {
        private readonly ILogger<ShipLogsLoop> _logger;
        private readonly AppSettings _appSettings;
        private readonly RabbitConnectionManager _rabbitConnectionManager;

        private SocketIO _socketClient;

        public ShipLogsLoop(ILogger<ShipLogsLoop> logger, AppSettings appSettings, RabbitConnectionManager rabbitConnectionManager)
        {
            _logger = logger;
            _appSettings = appSettings;
            _rabbitConnectionManager = rabbitConnectionManager;
        }

        public async void StartLoop(CancellationToken cancellationToken)
        {
            await ConnectToSocketIO(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await SendLogsToSocketIO();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred in ShipLogsLoop");
                }
                finally
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        private async Task ConnectToSocketIO(CancellationToken cancellationToken)
        {
            while (_socketClient == null && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var socketClient = new SocketIO(_appSettings.SocketioEndpoint);
                    await socketClient.ConnectAsync();
                    _socketClient = socketClient;
                    _logger.LogDebug("Connection to Socket.IO established");
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "An error occurred connecting to Socket.IO, will retry connection in 10 seconds");
                    await Task.Delay(10000, cancellationToken);
                }
            }
        }

        private async Task SendLogsToSocketIO()
        {

            _logger.LogTrace("Beginning SendLogsToSocketIO()");

            _rabbitConnectionManager.RabbitHelper.QueueDeclare<LogEntryMessage>();
            using var messageBatchAcker = _rabbitConnectionManager.RabbitHelper.RetrieveMessages<LogEntryMessage>(500);

            if (!messageBatchAcker.MessageContainers.Any())
            {
                return;
            }

            List<LogEntry> logEntries;

            try
            {
                var logEntryMessages = messageBatchAcker.MessageContainers.Select(x => x.Message).ToList();
                logEntries = logEntryMessages
                    .Select(x =>
                    {
                        try
                        {
                            return new LogEntry(x.JournalLogEntry);
                        }
                        catch (Exception e)
                        {
                            _logger.LogWarning(e, "An error occurred converting JournalLogEntry to LogEntry");
                            return null;
                        }
                    })
                    .Where(x => x != null)
                    .ToList();
            }
            catch (Exception)
            {
                // if the error occurs during some type of parsing, don't requeue
                _logger.LogWarning("An error occurred and log messages were not requeued, error to follow");
                messageBatchAcker.Nack(false);
                throw;
            }

            try
            {
                await TimeoutHelper.RunWithTimeout(ct => SendLogEntriesToSocketIO(logEntries, ct), 10000);
                messageBatchAcker.Ack();
            }
            catch (Exception)
            {
                // if the error occurs during sending to api, requeue
                _logger.LogWarning("An error occurred and log messages were requeued, error to follow");
                messageBatchAcker.Nack(true);
                throw;
            }
        }

        private Task SendLogEntriesToSocketIO(IEnumerable<LogEntry> logEntries, CancellationToken cancellationToken)
        {

            var taskCompletionSource = new TaskCompletionSource<object>();

            _socketClient.EmitAsync("agent/server/data", response =>
            {
                taskCompletionSource.SetResult(null);
            }, new { demoControllerId = _appSettings.DemoControllerId, logEntries });

            return taskCompletionSource.Task;
        }
    }
}