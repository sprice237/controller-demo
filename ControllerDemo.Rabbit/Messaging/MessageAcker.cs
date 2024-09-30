using System;
using RabbitMQ.Client;

namespace ControllerDemo.Rabbit.Messaging
{
    public class MessageAcker<TMessage> : IDisposable
    {
        public MessageContainer<TMessage> MessageContainer { get; }

        private IModel _channel;
        private readonly ulong _deliveryTag;

        public MessageAcker(IModel channel, ulong deliveryTag, MessageContainer<TMessage> messageContainer)
        {
            _channel = channel;
            _deliveryTag = deliveryTag;
            MessageContainer = messageContainer;
        }

        public void Ack()
        {
            _channel.BasicAck(_deliveryTag, false);
        }

        public void Nack(bool requeue)
        {
            _channel.BasicNack(_deliveryTag, false, requeue);
        }

        public void Dispose()
        {
            _channel = null;
        }
    }
}