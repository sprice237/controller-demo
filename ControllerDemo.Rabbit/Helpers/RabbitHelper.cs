using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ControllerDemo.Common;
using ControllerDemo.Rabbit.Consumers;
using ControllerDemo.Rabbit.Exceptions;
using ControllerDemo.Rabbit.Loops;
using ControllerDemo.Rabbit.Messaging;
using RabbitMQ.Client;

namespace ControllerDemo.Rabbit.Helpers
{
    public static class ChannelWrapperHelpers
    {
        public static DisposableChannelWrapper CreateDisposableChannelWrapper(this IConnection connection)
        {
            var disposableChannel = new DisposableChannelWrapper(connection);
            return disposableChannel;
        }
    }

    public class DisposableChannelWrapper : IDisposable
    {
        public IModel Channel { get; }

        public DisposableChannelWrapper(IConnection connection)
        {
            Channel = connection.CreateModel();
        }
        public void Dispose()
        {
            Channel?.Close();
            Channel?.Dispose();
        }
    }
    public class RabbitHelper
    {
        private readonly ILogger<RabbitHelper> _logger;
        private readonly RabbitConnectionLoop _rabbitConnectionLoop;
        private readonly ResponseConsumerFactory _responseConsumerFactory;

        public RabbitHelper(ILogger<RabbitHelper> logger, RabbitConnectionLoop rabbitConnectionLoop, ResponseConsumerFactory responseConsumerFactory)
        {
            _logger = logger;
            _rabbitConnectionLoop = rabbitConnectionLoop;
            _responseConsumerFactory = responseConsumerFactory;
        }

        public void QueueDeclare<TMessage>(bool durable = true, bool exclusive = false, bool autoDelete = false) where TMessage : BaseMessage, new()
        {
            if (_rabbitConnectionLoop.Connection == null)
            {
                throw new RabbitNotConnectedException();
            }

            using var channelWrapper = _rabbitConnectionLoop.Connection.CreateDisposableChannelWrapper();
            var channel = channelWrapper.Channel;

            var queueName = new TMessage().QueueName;

            channel.QueueDeclare(
                queueName,
                durable,
                exclusive,
                autoDelete
            );
        }


        public void SendMessage<TMessage>(TMessage message, bool gracefulFailure = false) where TMessage : BaseMessage
        {
            try
            {
                SendMessage(message.QueueName, message);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Send message to queue {message.QueueName} failed");
                if (!gracefulFailure)
                {
                    throw;
                }
            }
        }

        public async Task<Tuple<TResponse, string>> SendMessageWithResponse<TMessage, TResponse>(TMessage message, CancellationToken cancellationToken, Func<TResponse, string, Task> acknowledgementHandler = null) where TMessage : BaseMessage where TResponse : BaseResponse, new()
        {
            using var responseConsumer = _responseConsumerFactory.SendWithResponse<TMessage, TResponse>(_rabbitConnectionLoop.Connection, message);

            await foreach (var messageAcker in responseConsumer.GetResponses(cancellationToken))
            {
                if (messageAcker.MessageContainer.Message.IsAcknowledgement)
                {
                    if (acknowledgementHandler != null)
                    {
                        await acknowledgementHandler(messageAcker.MessageContainer.Message, messageAcker.MessageContainer.MessageJson);
                    }

                    messageAcker.Ack();
                    continue;
                }

                if (messageAcker.MessageContainer.Message.IsSuccessful)
                {
                    messageAcker.Ack();
                    return new Tuple<TResponse, string>(messageAcker.MessageContainer.Message, messageAcker.MessageContainer.MessageJson);
                }

                if (messageAcker.MessageContainer.Message.IsException)
                {
                    throw new ExceptionResponseException(messageAcker.MessageContainer.Message);
                }

                throw new Exception("Response was not acknowledgement, successful, or exception");
            }

            throw new Exception("Never received successful or exception response");
        }

        public void SendMessage<TMessage>(string queueName, TMessage message)
        {
            if (_rabbitConnectionLoop.Connection == null)
            {
                throw new RabbitNotConnectedException();
            }

            using var channelWrapper = _rabbitConnectionLoop.Connection.CreateDisposableChannelWrapper();
            var channel = channelWrapper.Channel;

            var outgoingMessageJson = message.SerializeJson();
            var outgoingMessageBytes = Encoding.UTF8.GetBytes(outgoingMessageJson);

            var basicProperties = channel.CreateBasicProperties();
            basicProperties.Persistent = true;

            channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: basicProperties, body: outgoingMessageBytes);
        }

        public MessageBatchAcker<TMessage> RetrieveMessages<TMessage>(ushort maxNumMessages) where TMessage : BaseMessage, new()
        {
            if (_rabbitConnectionLoop.Connection == null)
            {
                throw new RabbitNotConnectedException();
            }

            var channelWrapper = _rabbitConnectionLoop.Connection.CreateDisposableChannelWrapper();

            var queueName = new TMessage().QueueName;

            var messageContainers = new List<MessageContainer<TMessage>>();
            ulong? lastDeliveryTag = null;
            while (messageContainers.Count < maxNumMessages)
            {
                var messageGetResult = channelWrapper.Channel.BasicGet(queueName, false);
                if (messageGetResult == null)
                {
                    break;
                }

                lastDeliveryTag = messageGetResult.DeliveryTag;
                var messageContainer = new MessageContainer<TMessage>(messageGetResult.Body)
                {
                    BasicProperties = messageGetResult.BasicProperties
                };
                messageContainers.Add(messageContainer);
            }

            var messageBatchAcker = new MessageBatchAcker<TMessage>(channelWrapper.Channel, lastDeliveryTag, messageContainers, true);

            return messageBatchAcker;
        }
    }
}