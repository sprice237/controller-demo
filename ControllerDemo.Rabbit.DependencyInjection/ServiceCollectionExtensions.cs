using Microsoft.Extensions.DependencyInjection;
using ControllerDemo.Rabbit.Consumers;
using ControllerDemo.Rabbit.Helpers;
using ControllerDemo.Rabbit.Loops;

namespace ControllerDemo.Rabbit.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void ApplyRabbitServiceCollectionExtensions(this IServiceCollection serviceCollection, IRabbitAppSettings appSettings)
        {
            serviceCollection.AddSingleton<IRabbitAppSettings, IRabbitAppSettings>(_ => appSettings);
            serviceCollection.AddSingleton<RabbitConsumerHelper, RabbitConsumerHelper>();
            serviceCollection.AddSingleton<RabbitHelper, RabbitHelper>();
            serviceCollection.AddSingleton<RabbitConnectionLoop, RabbitConnectionLoop>();
            serviceCollection.AddSingleton<RabbitConnectionManager, RabbitConnectionManager>();
            serviceCollection.AddSingleton<ResponseConsumerFactory, ResponseConsumerFactory>();
            serviceCollection.AddSingleton<SubscribeConsumerFactory, SubscribeConsumerFactory>();
        }
    }
}