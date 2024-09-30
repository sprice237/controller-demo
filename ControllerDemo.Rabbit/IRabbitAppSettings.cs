namespace ControllerDemo.Rabbit
{
    public interface IRabbitAppSettings
    {
        string MessageQueueHostName { get; }
        int MessageQueuePort { get; }
        string MessageQueueUserName { get; }
        string MessageQueuePassword { get; }
        int MessageQueueConnectionRetryIntervalMilliseconds { get; }
    }
}