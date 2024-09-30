using System;
using System.Text;
using System.Text.Json;
using ControllerDemo.Common;
using ControllerDemo.Rabbit.Exceptions;
using RabbitMQ.Client;

namespace ControllerDemo.Rabbit.Messaging
{
    public class MessageContainer<TMessage>
    {
        public static Tuple<TMessage, string> ParseMessage(string body)
        {
            try
            {
                var message = body.DeserializeJson<TMessage>();
                return new Tuple<TMessage, string>(message, body);
            }
            catch (JsonException e)
            {
                throw new MessageParseException(body, e);
            }
        }

        private bool _messageParsed;
        private TMessage _message;
        private string _messageJson;
        public string Body { get; set; }
        public Exception MessageParseException { get; set; }

        public TMessage Message => _messageParsed ? _message : ParseMessage()._message;

        public string MessageJson => _messageParsed ? _messageJson : ParseMessage()._messageJson;
        public IBasicProperties BasicProperties { get; set; }

        public MessageContainer()
        {

        }

        public MessageContainer(ReadOnlyMemory<byte> body)
        {
            var messageBytes = body.ToArray();
            Body = Encoding.UTF8.GetString(messageBytes);
        }

        private MessageContainer<TMessage> ParseMessage()
        {
            try
            {
                (_message, _messageJson) = ParseMessage(Body);
            }
            catch (Exception e)
            {
                MessageParseException = e;
                _messageParsed = true;
            }

            return this;
        }
    }
}