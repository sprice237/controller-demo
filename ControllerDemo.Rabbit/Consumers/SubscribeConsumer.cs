using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ControllerDemo.Rabbit.Messaging;
using RabbitMQ.Client;

namespace ControllerDemo.Rabbit.Consumers
{
    public class SubscribeConsumerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SubscribeConsumerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public SubscribeConsumer<TResponse> Build<TResponse>(IConnection connection, ushort prefetch) where TResponse : BaseMessage, new()
        {
            var queueName = (new TResponse()).QueueName;

            var channel = connection.CreateModel();
            channel.BasicQos(0, prefetch, false);
            channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            var subscribeConsumerLogger = (ILogger<SubscribeConsumer<TResponse>>)_serviceProvider.GetService(typeof(ILogger<SubscribeConsumer<TResponse>>));
            var subscribeConsumer = new SubscribeConsumer<TResponse>(subscribeConsumerLogger, channel, queueName);

            return subscribeConsumer;
        }
    }

    public class SubscribeConsumer<TResponse> : BaseConsumer where TResponse : BaseMessage, new()
    {
        private readonly ILogger<SubscribeConsumer<TResponse>> _logger;

        public delegate Task OnMessageAsyncHandler(MessageAcker<TResponse> messageAcker);
        public event OnMessageAsyncHandler OnMessageAsync;

        public SubscribeConsumer(ILogger<SubscribeConsumer<TResponse>> logger, IModel model, string queueName) : base(model, true)
        {
            _logger = logger;

            _consumerTag = model.BasicConsume(
                queue: queueName,
                autoAck: false,
                consumer: this
            );
        }

        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> body)
        {
            if (OnMessageAsync == null)
            {
                Model.BasicNack(deliveryTag, false, true);
                return;
            }

            try
            {
                var messageContainer = new MessageContainer<TResponse>(body)
                {
                    BasicProperties = properties
                };

                var messageAcker = new MessageAcker<TResponse>(Model, deliveryTag, messageContainer);

                OnMessageAsync(messageAcker);
            }
            catch (Exception e)
            {
                try
                {
                    Model.BasicReject(deliveryTag, false);
                }
                catch (Exception rejectException)
                {
                    e = new AggregateException(e, rejectException);
                }

                _logger.LogError(e, "An unhandled error occurred in SubscribeConsumer.HandleBasicDeliver");
            }
        }
    }
}