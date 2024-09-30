using System;

namespace ControllerDemo.Rabbit.Exceptions
{
    public class ExceptionResponseException : Exception
    {
        public BaseResponse Response { get; }

        public ExceptionResponseException(BaseResponse response)
        {
            Response = response;
        }
    }
}