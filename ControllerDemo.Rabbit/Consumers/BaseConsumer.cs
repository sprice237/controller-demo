using System;
using System.Text;
using System.Text.Json;
using ControllerDemo.Common;
using ControllerDemo.Rabbit.Exceptions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ControllerDemo.Rabbit.Consumers
{
    public interface IResponseConsumer : IDisposable
    {
        event EventHandler<ConsumerEventArgs> ConsumerCancelled;
    }

    public abstract class BaseConsumer : DefaultBasicConsumer, IResponseConsumer
    {
        private readonly bool _closeChannelOnDispose;
        protected string _consumerTag;
        protected BaseConsumer(IModel model, bool closeChannelOnDispose) : base(model)
        {
            _closeChannelOnDispose = closeChannelOnDispose;
        }

        public virtual void Dispose()
        {
            if (_consumerTag != null)
            {
                Model.BasicCancel(_consumerTag); // it is necessary to manually cancel the consumer, otherwise Model doesn't get picked up for garbage collection
            }

            if (_closeChannelOnDispose)
            {
                Model.Close();
                Model.Dispose();
                Model = null;
            }
        }
    }
}