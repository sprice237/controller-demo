using System;

namespace ControllerDemo.Rabbit
{
    public class BaseMessage
    {
        public virtual string QueueName
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}