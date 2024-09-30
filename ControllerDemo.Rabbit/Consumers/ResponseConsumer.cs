using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ControllerDemo.Common;
using ControllerDemo.Rabbit.Messaging;
using RabbitMQ.Client;

namespace ControllerDemo.Rabbit.Consumers
{
    public class ResponseConsumerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ResponseConsumerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ResponseConsumer<TResponse> SendWithResponse<TMessage, TResponse>(IConnection connection, TMessage message) where TMessage : BaseMessage where TResponse : BaseMessage, new()
        {
            var responseQueueName = $"response-queue-{Guid.NewGuid()}";

            var channel = connection.CreateModel();
            channel.BasicQos(0, 1, false);
            channel.QueueDeclare(responseQueueName, exclusive: false, autoDelete: true);

            var outgoingMessageJson = message.SerializeJson();
            var outgoingMessageBytes = Encoding.UTF8.GetBytes(outgoingMessageJson);

            var responseConsumerLogger = (ILogger<ResponseConsumer<TResponse>>)_serviceProvider.GetService(typeof(ILogger<ResponseConsumer<TResponse>>));
            var responseConsumer = new ResponseConsumer<TResponse>(responseConsumerLogger, channel, responseQueueName);

            var basicProperties = channel.CreateBasicProperties();
            basicProperties.ReplyTo = responseQueueName;
            basicProperties.Persistent = true;
            channel.BasicPublish(exchange: "", routingKey: message.QueueName, basicProperties: basicProperties, body: outgoingMessageBytes);

            return responseConsumer;
        }
    }

    public class ResponseConsumer<TResponse> : BaseConsumer where TResponse : BaseMessage, new()
    {
        private readonly ILogger<ResponseConsumer<TResponse>> _logger;
        private readonly ConcurrentQueue<MessageAcker<TResponse>> _incomingMessagesQueue = new ConcurrentQueue<MessageAcker<TResponse>>();
        private TaskCompletionSource<object> _newMessageTaskCompletionSource;

        public ResponseConsumer(ILogger<ResponseConsumer<TResponse>> logger, IModel model, string responseQueueName) : base(model, true)
        {
            _logger = logger;
            _newMessageTaskCompletionSource = new TaskCompletionSource<object>();

            _consumerTag = model.BasicConsume(
                queue: responseQueueName,
                autoAck: false,
                consumer: this
            );
        }

        public async IAsyncEnumerable<MessageAcker<TResponse>> GetResponses([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var cancellationTaskCompletionSource = new TaskCompletionSource<object>();
            await using var cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                cancellationTaskCompletionSource.SetResult(null);
            });

            while (!cancellationToken.IsCancellationRequested && Model.IsOpen)
            {
                // wait for a new message or cancellation request, whichever comes first
                var winningTask = await Task.WhenAny(_newMessageTaskCompletionSource.Task, cancellationTaskCompletionSource.Task);
                if (winningTask == cancellationTaskCompletionSource.Task)
                {
                    // if the cancellation happened first, gtfo
                    yield break;
                }

                // loop through messages in queue
                while (_incomingMessagesQueue.TryDequeue(out var messageAcker))
                {
                    await Task.Yield(); // there are deadlocking issues in the c# rabbitmq client, without this line the service can 
                                        // hang at certain times (like retrieving point values). adding this line apparently gives
                                        // the rabbitmq client enough time to clean things up to keep things functioning

                    // if cancellation is requested, gtfo
                    if (cancellationToken.IsCancellationRequested)
                    {
                        yield break;
                    }

                    // yield response wrapper
                    yield return messageAcker;
                }

                // create new _newMessageTaskCompletionSource to be alerted of new messages
                _newMessageTaskCompletionSource = new TaskCompletionSource<object>();
            }
        }

        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> body)
        {
            var messageContainer = new MessageContainer<TResponse>(body)
            {
                BasicProperties = properties
            };

            var messageAcker = new MessageAcker<TResponse>(Model, deliveryTag, messageContainer);

            _incomingMessagesQueue.Enqueue(messageAcker);
            if (!_newMessageTaskCompletionSource.Task.IsCompleted)
            {
                _newMessageTaskCompletionSource.SetResult(null);
            }
        }
    }
}