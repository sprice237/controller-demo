using System.IO;
using System.Threading.Tasks;
using ControllerDemo.Common;
using ControllerDemo.Rabbit;

namespace ControllerDemo.Services.LogTransportService
{
    public class AppSettings : BaseAppSettings, IRabbitAppSettings
    {
        // all of these properties must have public setters to work with the .NET core configuration provider

        public string SocketioEndpointFilePath { get; set; }
        public string SocketioEndpoint { get; set; }
        public string LastCursorFilePath { get; set; }
        public string MessageQueueHostName { get; set; } = "localhost";
        public int MessageQueuePort { get; set; } = 5672;
        public string MessageQueueUserName { get; set; } = "sean_demo";
        public string MessageQueuePassword { get; set; } = "sean_demo";
        public int MessageQueueConnectionRetryIntervalMilliseconds { get; } = 15000;

        public override async Task FillAdditionalDynamicProperties()
        {
            SocketioEndpoint = await GetSocketioEndpoint();
        }

        private async Task<string> GetSocketioEndpoint()
        {
            using (var stream = new FileStream(SocketioEndpointFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var streamReader = new StreamReader(stream))
            {
                var apiUrl = (await streamReader.ReadToEndAsync()).Trim();
                return $"{apiUrl}/ControllerDemo.Services.LogTransportService";
            }
        }
    }
}