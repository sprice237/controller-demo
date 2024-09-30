using System;

namespace ControllerDemo.Rabbit
{
    public class BaseResponse : BaseMessage
    {
        public bool IsException { get; set; }
        public bool IsSuccessful { get; set; }
        public bool IsAcknowledgement { get; set; }
        public Guid? ExceptionCorrelationId { get; set; }
        public string ExceptionType { get; set; }
        public string ExceptionMessage { get; set; }
    }
}