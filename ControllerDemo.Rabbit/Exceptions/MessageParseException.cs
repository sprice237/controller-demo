using System;

namespace ControllerDemo.Rabbit.Exceptions
{
    public class MessageParseException : Exception
    {
        public MessageParseException(string messageJson = "", Exception innerException = null) : base($"Could not parse message {messageJson}", innerException)
        {
        }
    }
}