using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ControllerDemo.Common;

namespace ControllerDemo.Services.Common
{
    public abstract class BaseProgram<TWorker, TAppSettings> where TAppSettings : BaseAppSettings where TWorker : BaseWorker<TAppSettings>
    {
        protected async Task Start(string[] args)
        {
            Console.WriteLine("BaseProgram.Start()");

            var hostBuilder = CreateHostBuilder(args);
            var host = hostBuilder.Build();
            await host.RunAsync();
        }

        private IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureServices((hostContext, services) =>
                {
                    var appSettings = hostContext.Configuration.Get<TAppSettings>();

                    appSettings.FillDynamicProperties().Wait();

                    services.AddSingleton<BaseAppSettings, BaseAppSettings>(_ => appSettings);
                    services.AddSingleton<TAppSettings, TAppSettings>(_ => appSettings);
                    ApplyServiceCollectionEnhancements(services, appSettings);

                    services.AddHostedService<TWorker>();
                });
        }

        protected virtual void ApplyServiceCollectionEnhancements(IServiceCollection serviceCollection, TAppSettings appSettings)
        {

        }
    }
}