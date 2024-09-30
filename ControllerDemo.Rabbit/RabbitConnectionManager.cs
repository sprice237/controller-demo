using System.Threading;
using Microsoft.Extensions.Logging;
using ControllerDemo.Rabbit.Helpers;
using ControllerDemo.Rabbit.Loops;

namespace ControllerDemo.Rabbit
{
    public class RabbitConnectionManager
    {
        public RabbitConsumerHelper RabbitConsumerHelper { get; }
        public RabbitHelper RabbitHelper { get; }

        private readonly ILogger<RabbitConnectionManager> _logger;
        private readonly RabbitConnectionLoop _rabbitConnectionLoop;

        public RabbitConnectionManager(ILogger<RabbitConnectionManager> logger, RabbitConnectionLoop rabbitConnectionLoop, RabbitConsumerHelper rabbitConsumerHelper, RabbitHelper rabbitHelper)
        {
            RabbitConsumerHelper = rabbitConsumerHelper;
            RabbitHelper = rabbitHelper;

            _logger = logger;
            _rabbitConnectionLoop = rabbitConnectionLoop;
        }

        public void Start(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting RabbitConnectionManager");

            _rabbitConnectionLoop.StartLoop(cancellationToken);
            RabbitConsumerHelper.Start(cancellationToken);
        }
    }
}