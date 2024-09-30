using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ControllerDemo.ApiConnector;
using ControllerDemo.Rabbit;
using ControllerDemo.Services.Common;
using ControllerDemo.Services.LogTransportService.Loops;

namespace ControllerDemo.Services.LogTransportService
{
    public class Worker : BaseWorker<AppSettings>
    {
        private readonly RabbitConnectionManager _rabbitConnectionManager;
        private readonly JournalLogsLoop _journalLogsLoop;
        private readonly ShipLogsLoop _shipLogsLoop;

        public Worker(ILogger<BaseWorker<AppSettings>> baseServiceLogger, AppSettings appSettings, BaseApiHelper apiHelper, RabbitConnectionManager rabbitConnectionManager, JournalLogsLoop journalLogsLoop, ShipLogsLoop shipLogsLoop) : base(baseServiceLogger, appSettings, apiHelper)
        {
            _rabbitConnectionManager = rabbitConnectionManager;
            _journalLogsLoop = journalLogsLoop;
            _shipLogsLoop = shipLogsLoop;
        }

        protected override async Task Main(CancellationToken cancellationToken)
        {
            _rabbitConnectionManager.Start(cancellationToken);

            _journalLogsLoop.StartLoop(cancellationToken);
            _shipLogsLoop.StartLoop(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(5000, cancellationToken);
            }
        }
    }
}