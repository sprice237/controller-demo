using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace ControllerDemo.Rabbit.Messaging
{
    public class MessageBatchAcker<TMessage> : IDisposable
    {
        public List<MessageContainer<TMessage>> MessageContainers { get; }

        private IModel _channel;
        private readonly ulong? _lastDeliveryTag;
        private readonly bool _closeChannelOnDispose;

        public MessageBatchAcker(IModel channel, ulong? lastDeliveryTag, List<MessageContainer<TMessage>> messageContainers, bool closeChannelOnDispose)
        {
            _channel = channel;
            _lastDeliveryTag = lastDeliveryTag;
            _closeChannelOnDispose = closeChannelOnDispose;
            MessageContainers = messageContainers;
        }

        public void Ack()
        {
            if (_lastDeliveryTag != null)
            {
                _channel.BasicAck(_lastDeliveryTag.Value, true);
            }
        }

        public void Nack(bool requeue)
        {
            if (_lastDeliveryTag != null)
            {
                _channel.BasicNack(_lastDeliveryTag.Value, true, requeue);
            }
        }

        public void Dispose()
        {
            if (_closeChannelOnDispose)
            {
                _channel.Close();
                _channel.Dispose();
            }
            _channel = null;
        }
    }
}