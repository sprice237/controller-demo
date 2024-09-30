using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ControllerDemo.ApiConnector;
using ControllerDemo.Rabbit.DependencyInjection;
using ControllerDemo.Services.Common;
using ControllerDemo.Services.LogTransportService.Helpers;
using ControllerDemo.Services.LogTransportService.Loops;

namespace ControllerDemo.Services.LogTransportService
{
    class Program : BaseProgram<Worker, AppSettings>
    {
        static async Task Main(string[] args)
        {
            var program = new Program();
            await program.Start(args);
        }

        protected override void ApplyServiceCollectionEnhancements(IServiceCollection serviceCollection, AppSettings appSettings)
        {
            serviceCollection.ApplyRabbitServiceCollectionExtensions(appSettings);

            // Service
            serviceCollection.AddSingleton<BaseApiHelper, BaseApiHelper>();
            serviceCollection.AddSingleton<JournalLogsLoop, JournalLogsLoop>();
            serviceCollection.AddSingleton<OfflineCachedLogsHelper, OfflineCachedLogsHelper>();
            serviceCollection.AddSingleton<ShipLogsLoop, ShipLogsLoop>();
        }
    }
}