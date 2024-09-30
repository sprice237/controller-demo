using System;
using System.IO;
using System.Threading.Tasks;

namespace ControllerDemo.Common
{
    public class BaseAppSettings
    {
        public virtual string DemoControllerIdFilePath { get; set; }
        public virtual Guid DemoControllerId { get; set; }
        public virtual string ApiUrlFilePath { get; set; }
        public virtual string ApiUrl { get; set; }
        public virtual string Version { get; set; }
        public virtual string ServiceId { get; set; }

        public async Task FillDynamicProperties()
        {
            DemoControllerId = await GetDemoControllerId();
            ApiUrl = await GetApiUrl();
#if DEBUG
            Version = "debug";
#else
            Version = await GetVersion();
#endif
            await FillAdditionalDynamicProperties();
        }

        public virtual async Task FillAdditionalDynamicProperties()
        {
            await Task.CompletedTask;
        }

        private async Task<Guid> GetDemoControllerId()
        {
            using (var stream = new FileStream(DemoControllerIdFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var streamReader = new StreamReader(stream))
            {
                var demoControllerId = Guid.Parse(await streamReader.ReadToEndAsync());
                return demoControllerId;
            }
        }

        private async Task<string> GetApiUrl()
        {
            using (var stream = new FileStream(ApiUrlFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var streamReader = new StreamReader(stream))
            {
                var apiUrl = (await streamReader.ReadToEndAsync()).Trim();
                return apiUrl;
            }
        }

        private static async Task<string> GetVersion()
        {
            var versionFilePath = Path.Join(Directory.GetCurrentDirectory(), "Deployment", "version");
            using (var stream = new FileStream(versionFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var streamReader = new StreamReader(stream))
            {
                var version = (await streamReader.ReadToEndAsync()).Trim();
                return version;
            }
        }
    }
}