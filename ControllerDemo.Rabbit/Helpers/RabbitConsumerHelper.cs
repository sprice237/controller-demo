using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ControllerDemo.Rabbit.Consumers;
using ControllerDemo.Rabbit.Loops;
using ControllerDemo.Rabbit.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ControllerDemo.Rabbit.Helpers
{

    interface IRabbitConsumerDefinition
    {
        Guid Id { get; }
        string QueueName { get; }
        IResponseConsumer Consumer { get; }
        bool IsConsumerNull { get; }
        void DisposeConsumer();
        void BuildConsumer(IConnection connection);
    }

    class RabbitConsumerDefinition<TMessage> : IRabbitConsumerDefinition where TMessage : BaseMessage, new()
    {
        public Guid Id { get; } = Guid.NewGuid();
        public IResponseConsumer Consumer { get; set; }
        public bool IsConsumerNull => Consumer == null;
        public string QueueName => new TMessage().QueueName;
        private readonly Func<IConnection, BaseConsumer> _consumerConstructor;

        public RabbitConsumerDefinition(Func<IConnection, BaseConsumer> consumerConstructor)
        {
            _consumerConstructor = consumerConstructor;
        }

        public void BuildConsumer(IConnection connection)
        {
            Consumer = _consumerConstructor(connection);
        }

        public void DisposeConsumer()
        {
            Consumer.Dispose();
            Consumer = null;
        }
    }

    public class RabbitConsumerHelper
    {
        private readonly ILogger<RabbitConsumerHelper> _logger;
        private readonly RabbitConnectionLoop _rabbitConnectionLoop;
        private readonly SubscribeConsumerFactory _subscribeConsumerFactory;

        private readonly Dictionary<Guid, IRabbitConsumerDefinition> _consumerDefinitions = new Dictionary<Guid, IRabbitConsumerDefinition>();

        public RabbitConsumerHelper(ILogger<RabbitConsumerHelper> logger, RabbitConnectionLoop rabbitConnectionLoop, SubscribeConsumerFactory subscribeConsumerFactory)
        {
            _logger = logger;
            _rabbitConnectionLoop = rabbitConnectionLoop;
            _subscribeConsumerFactory = subscribeConsumerFactory;
        }

        public void Start(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting ConsumerHelper");

            _rabbitConnectionLoop.OnConnectionEstablished(TryCreateConsumersFromAllConsumerDefinitions);

            _rabbitConnectionLoop.OnConnectionShutdown(() =>
            {
                foreach (var consumerDefinition in _consumerDefinitions.Values)
                {
                    consumerDefinition.DisposeConsumer();
                }
            });

            _rabbitConnectionLoop.StartLoop(cancellationToken);
        }

        private void TryCreateConsumersFromAllConsumerDefinitions()
        {
            var unattachedConsumerDefinitions = _consumerDefinitions
                .Values
                .Where(x => x.IsConsumerNull);

            foreach (var consumerDefinition in unattachedConsumerDefinitions)
            {
                TryCreateConsumerFromConsumerDefinition(consumerDefinition);
            }
        }

        private void TryCreateConsumerFromConsumerDefinition(IRabbitConsumerDefinition consumerDefinition)
        {
            if (_rabbitConnectionLoop.Connection == null)
            {
                return;
            }

            var queueName = consumerDefinition.QueueName;

            try
            {
                consumerDefinition.BuildConsumer(_rabbitConnectionLoop.Connection);

                void ConsumerCancelledEventHandler(object sender, ConsumerEventArgs args)
                {
                    consumerDefinition.DisposeConsumer();

                    if (!_consumerDefinitions.ContainsKey(consumerDefinition.Id))
                    {
                        // this consumer has been removed from the list of _consumerDefinitions,
                        // we no longer care about it, disconnect the ConsumerCancelled event handler
                        consumerDefinition.Consumer.ConsumerCancelled -= ConsumerCancelledEventHandler;
                        return;
                    }

                    _logger.LogError($"Consumer for queue {queueName} disconnected unexpectedly");
                }

                consumerDefinition.Consumer.ConsumerCancelled += ConsumerCancelledEventHandler;
                _logger.LogInformation($"Added consumer for queue {queueName}");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Unable to add consumer for queue {queueName}");
            }
        }


        public Guid RegisterConsumer<TMessage>(Func<MessageAcker<TMessage>, Task> handler, ushort prefetchCount = 0) where TMessage : BaseMessage, new()
        {
            var consumerDefinition = new RabbitConsumerDefinition<TMessage>(
                connection =>
                {
                    var consumer = _subscribeConsumerFactory.Build<TMessage>(connection, prefetchCount);
                    consumer.OnMessageAsync += messageWrapper => handler(messageWrapper);
                    return consumer;
                }
            );

            _consumerDefinitions.Add(consumerDefinition.Id, consumerDefinition);
            TryCreateConsumerFromConsumerDefinition(consumerDefinition);
            return consumerDefinition.Id;
        }


        public void UnregisterConsumer(Guid consumerDefinitionId)
        {
            if (!_consumerDefinitions.ContainsKey(consumerDefinitionId))
            {
                return;
            }

            var consumerDefinition = _consumerDefinitions[consumerDefinitionId];
            if (consumerDefinition.Consumer != null)
            {
                consumerDefinition.DisposeConsumer();
            }

            _consumerDefinitions.Remove(consumerDefinition.Id);
        }
    }
}