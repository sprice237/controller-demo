using System;

namespace ControllerDemo.Rabbit.Exceptions
{
    public class RabbitNotConnectedException : Exception
    {
        public RabbitNotConnectedException() : base("Rabbit not connected")
        {

        }
    }
}