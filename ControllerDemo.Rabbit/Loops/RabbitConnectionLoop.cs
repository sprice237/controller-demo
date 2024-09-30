using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace ControllerDemo.Rabbit.Loops
{
    public class RabbitConnectionLoop
    {
        private readonly ILogger<RabbitConnectionLoop> _logger;
        private readonly IRabbitAppSettings _appSettings;
        public IConnection Connection { get; private set; }

        public delegate void ConnectionEstablishedHandler();
        private event ConnectionEstablishedHandler ConnectionEstablishedEvent;

        public delegate void ConnectionShutdownHandler();
        private event ConnectionShutdownHandler ConnectionShutdownEvent;

        public RabbitConnectionLoop(ILogger<RabbitConnectionLoop> logger, IRabbitAppSettings appSettings)
        {
            _logger = logger;
            _appSettings = appSettings;
        }

        public void StartLoop(CancellationToken cancellationToken)
        {
            EstablishConnectionLoop(cancellationToken);
        }

        private async void EstablishConnectionLoop(CancellationToken cancellationToken)
        {
            while (Connection == null && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug("Attempting to initiate connection to rabbit");
                    EstablishConnection(cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred attempting to obtain a connection to RabbitMQ");
                }

                await Task.Delay(_appSettings.MessageQueueConnectionRetryIntervalMilliseconds, cancellationToken);
            }
        }

        private void EstablishConnection(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _appSettings.MessageQueueHostName,
                Port = _appSettings.MessageQueuePort,
                UserName = _appSettings.MessageQueueUserName,
                Password = _appSettings.MessageQueuePassword
            };

            Connection = factory.CreateConnection();
            _logger.LogInformation("Established connection to RabbitMQ");
            Connection.ConnectionShutdown += (sender, args) => OnConnectionShutdownInternal(cancellationToken);
            ConnectionEstablishedEvent?.Invoke();
        }

        private void OnConnectionShutdownInternal(CancellationToken cancellationToken)
        {
            _logger.LogError("Lost connection to RabbitMQ");
            ConnectionShutdownEvent?.Invoke();

            Connection?.Dispose();
            Connection = null;

            EstablishConnectionLoop(cancellationToken);
        }

        public void OnConnectionEstablished(ConnectionEstablishedHandler h)
        {
            if (Connection != null)
            {
                h();
            }

            ConnectionEstablishedEvent += h;
        }

        public void OnConnectionShutdown(ConnectionShutdownHandler h)
        {
            ConnectionShutdownEvent += h;
        }
    }
}