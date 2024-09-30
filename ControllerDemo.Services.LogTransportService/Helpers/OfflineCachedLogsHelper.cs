using System.IO;
using System.Threading.Tasks;

namespace ControllerDemo.Services.LogTransportService.Helpers
{
    public class OfflineCachedLogsHelper
    {
        private readonly AppSettings _appSettings;

        public OfflineCachedLogsHelper(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public async Task WriteLastCursor(string lastCursor)
        {
            await using var stream = new FileStream(_appSettings.LastCursorFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await using var streamWriter = new StreamWriter(stream);
            await streamWriter.WriteAsync(lastCursor);
        }

        public async Task<string> ReadLastCursor()
        {
            try
            {
                await using var stream = new FileStream(_appSettings.LastCursorFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                using var streamReader = new StreamReader(stream);
                var lastCursor = await streamReader.ReadToEndAsync();

                return lastCursor;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public void DeleteLastCursor()
        {
            try
            {
                File.Delete(_appSettings.LastCursorFilePath);
            }
            catch (FileNotFoundException)
            {
                // don't care
            }
        }
    }
}