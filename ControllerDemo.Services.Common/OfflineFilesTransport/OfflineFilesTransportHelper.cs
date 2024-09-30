using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ControllerDemo.Common;

namespace ControllerDemo.Services.Common.OfflineFilesTransport
{
    public abstract class OfflineFilesTransportHelper
    {
        private readonly ILogger<OfflineFilesTransportHelper> _logger;

        protected abstract string OfflineFilesDirectoryPath { get; }
        protected virtual int NumLinesPerBatch { get; } = 500;

        public OfflineFilesTransportHelper(ILogger<OfflineFilesTransportHelper> logger)
        {
            _logger = logger;
        }

        public async void StartLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await HandleOfflineFiles(cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred in OfflineFilesTransportHelper");
                }
                finally
                {
                    await Task.Delay(5000, cancellationToken);
                }
            }
        }

        private async Task HandleOfflineFiles(CancellationToken cancellationToken = default)
        {
            if (OfflineFilesDirectoryPath == null)
            {
                throw new Exception("_offlineFilesDirectoryPath is null");
            }

            var offlineFilePaths = Directory.GetFiles(OfflineFilesDirectoryPath).OrderBy(f => f).ToList();

            var isFirstFile = true;
            foreach (var filePath in offlineFilePaths)
            {
                if (!isFirstFile)
                {
                    await Task.Delay(100, cancellationToken);
                }
                await HandleOfflineFile(filePath, cancellationToken);
                isFirstFile = false;
            }
        }

        private async Task HandleOfflineFile(string filePath, CancellationToken cancellationToken = default)
        {
            var isFirstBatch = true;
            while (true)
            {
                if (!isFirstBatch)
                {
                    await Task.Delay(100, cancellationToken);
                }
                var transitFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                try
                {
                    using (await Locker.GetLocker(filePath).AcquireLock(cancellationToken))
                    {
                        await SharedFileHandlers.TrimLinesFromStartOfFile(filePath, transitFilePath, NumLinesPerBatch);
                    }
                }
                catch (FileNotFoundException)
                {
                    break;
                }

                using (await Locker.GetLocker(filePath).AcquireLock(cancellationToken))
                {
                    SharedFileHandlers.DeleteFileIfEmpty(filePath);
                }

                var lines = await RetrieveLinesFromFile(transitFilePath);

                if (!lines.Any())
                {
                    break;
                }

                try
                {
                    _logger.LogTrace($"Found {lines.Count} lines in file {transitFilePath}");
                    await TimeoutHelper.RunWithTimeout(ct => HandleLines(lines, ct), 10000);
                }
                catch (Exception e)
                {
                    if (e is TimeoutException)
                    {
                        _logger.LogWarning($"A timeout occurred handling file {transitFilePath}");
                    }
                    else
                    {
                        _logger.LogWarning(e, $"Encountered error handling file {transitFilePath}");
                    }

                    using (await Locker.GetLocker(filePath).AcquireLock(cancellationToken))
                    {
                        await SharedFileHandlers.PrependLinesToStartOfFile(transitFilePath, filePath);
                    }

                    break;
                }
                finally
                {
                    File.Delete(transitFilePath);
                }

                isFirstBatch = false;
            }
        }

        private async Task<List<string>> RetrieveLinesFromFile(string filePath)
        {
            var lines = new List<string>();

            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            using var fileStreamReader = new StreamReader(fileStream);

            while (!fileStreamReader.EndOfStream)
            {
                var line = await fileStreamReader.ReadLineAsync();
                lines.Add(line);
            }

            return lines;
        }

        protected abstract Task HandleLines(IEnumerable<string> lines, CancellationToken cancellationToken);
    }
}