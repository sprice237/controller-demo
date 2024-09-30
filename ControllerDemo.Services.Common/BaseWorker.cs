using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ControllerDemo.ApiConnector;
using ControllerDemo.Common;

namespace ControllerDemo.Services.Common
{
    public abstract class BaseWorker<TAppSettings> : BackgroundService where TAppSettings : BaseAppSettings
    {
        private readonly ILogger<BaseWorker<TAppSettings>> _logger;
        private readonly TAppSettings _appSettings;
        private readonly BaseApiHelper _apiHelper;

        protected BaseWorker(ILogger<BaseWorker<TAppSettings>> logger, TAppSettings appSettings, BaseApiHelper apiHelper)
        {
            _logger = logger;
            _appSettings = appSettings;
            _apiHelper = apiHelper;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.Log(LogLevel.Information, $"Starting {_appSettings.ServiceId} version {_appSettings.Version}");
                _logger.Log(LogLevel.Debug, JsonSerializer.Serialize(_appSettings, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
                _logger.Log(LogLevel.Debug, $"Demo controller id: {_appSettings.DemoControllerId}");
                _logger.Log(LogLevel.Debug, $"API Url: {_appSettings.ApiUrl}");

                StartHeartbeatThread(stoppingToken);

                _logger.Log(LogLevel.Trace, $"Starting service main function");
                await Main(stoppingToken);
                _logger.Log(LogLevel.Critical, "Service has ended unexpectedly, no exception was thrown");
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Critical, e, "Service has ended unexpectedly");
            }
        }

        private void StartHeartbeatThread(CancellationToken stoppingToken)
        {
            var thread = new Thread(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await _apiHelper.SendHeartbeat();
                    }
                    catch (Exception)
                    {
                        // do nothing... for multiple reasons
                        // 1. if the heartbeat is failing, other things will be failing and we don't want to increase the noise
                        // 2. this is on a separate thread as the rest of the service and we don't want to increase the odds
                        //    of having some type of locking conflict on the log file
                        // 3. a heartbeat failure should be immediately obvious in the api because the last heartbeat date
                        //    won't get updated
                    }

                    await Task.Delay(15000);
                }
            });

            thread.Start();
        }

        protected abstract Task Main(CancellationToken stoppingToken);
    }
}