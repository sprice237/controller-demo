using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ControllerDemo.Common;

namespace ControllerDemo.ApiConnector
{
    public class BaseApiHelper
    {
        private readonly BaseAppSettings _baseAppSettings;

        private Uri BaseUri => new Uri(_baseAppSettings.ApiUrl);

        public BaseApiHelper(BaseAppSettings baseAppSettings)
        {
            _baseAppSettings = baseAppSettings;
        }

        private static string StripTrailingSlash(string s)
        {
            return s[^1] == '/' ? s.Substring(0, s.Length - 1) : s;
        }

        private Uri BuildUri(string path)
        {
            var uriBuilder = new UriBuilder(BaseUri);
            uriBuilder.Path = StripTrailingSlash(uriBuilder.Path) + $"/demo-controllers/{_baseAppSettings.DemoControllerId}" + path;
            return uriBuilder.Uri;
        }

        protected async Task<string> ExecuteRequest(string path, string method = "GET", string payload = null)
        {
            var uri = BuildUri(path);

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = method;

            httpWebRequest.Headers.Add("x-service-id", _baseAppSettings.ServiceId);
            httpWebRequest.Headers.Add("x-service-version", _baseAppSettings.Version);

            if (payload != null)
            {
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    await streamWriter.WriteAsync(payload);
                }
            }

            using (var httpWebResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync())
            using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream() ?? throw new InvalidOperationException()))
            {
                var responseBody = await streamReader.ReadToEndAsync();
                return responseBody;
            }
        }

        public async Task SendHeartbeat()
        {
            await ExecuteRequest("/heartbeat", "POST");
        }
    }
}