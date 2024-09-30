using System;
using System.Collections.Generic;
using ControllerDemo.Rabbit.Helpers;
using ControllerDemo.Rabbit.Messaging;

namespace ControllerDemo.Rabbit
{
    public class RetrieveMessagesResult<TMessage> : IDisposable where TMessage : BaseMessage, new()
    {
        private readonly DisposableChannelWrapper _channelWrapper;
        private readonly ulong? _lastDeliveryTag;

        public RetrieveMessagesResult(DisposableChannelWrapper channelWrapper, ulong? lastDeliveryTag, MessageBatchAcker<TMessage> messageBatchAcker)
        {
            _channelWrapper = channelWrapper;
            _lastDeliveryTag = lastDeliveryTag;
            MessageBatchAcker = messageBatchAcker;
        }

        public MessageBatchAcker<TMessage> MessageBatchAcker { get; }

        public void Ack()
        {
            if (_lastDeliveryTag != null)
            {
                _channelWrapper?.Channel.BasicAck(_lastDeliveryTag.Value, true);
            }
        }

        public void Nack()
        {
            if (_lastDeliveryTag != null)
            {
                _channelWrapper?.Channel.BasicNack(_lastDeliveryTag.Value, true, true);
            }
        }

        public void Dispose()
        {
            _channelWrapper.Dispose();
        }
    }
}