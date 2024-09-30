using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ControllerDemo.Common
{
    public static class SharedFileHandlers
    {
        public static readonly Func<Exception, bool> IsRetryableException = (e => (e as IOException)?.Message.Contains("used by another process") ?? false);

        public static async Task AppendToFile(string filePath, string line)
        {
            await AppendToFile(filePath, new[] { line });
        }

        public static async Task AppendToFile(string filePath, IEnumerable<string> lines)
        {
            await using var fileStream = await RetryableHelpers.GetWithRetry(() => new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None), IsRetryableException);
            await using var streamWriter = new StreamWriter(fileStream);
            foreach (var line in lines)
            {
                await streamWriter.WriteLineAsync(line);
            }
        }

        public static async Task PrependLinesToStartOfFile(string sourceFilePath, string destinationFilePath)
        {
            var combinedTempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());


            try
            {
                // source file, destination file, and remainder temp file will be locked for the duration
                // of this code block (i.e. the try block)

                await using var sourceFileStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                await using var destinationFileStream = new FileStream(destinationFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                await using var combinedTempFileStream = new FileStream(combinedTempFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);


                using (var sourceFileStreamReader = new StreamReader(sourceFileStream, leaveOpen: true))
                using (var destinationFileStreamReader = new StreamReader(destinationFileStream, leaveOpen: true))
                await using (var combinedTempFileStreamWriter = new StreamWriter(combinedTempFileStream, leaveOpen: true))
                {
                    while (!sourceFileStreamReader.EndOfStream)
                    {
                        var line = await sourceFileStreamReader.ReadLineAsync();
                        await combinedTempFileStreamWriter.WriteLineAsync(line);
                    }

                    while (!destinationFileStreamReader.EndOfStream)
                    {
                        var line = await destinationFileStreamReader.ReadLineAsync();
                        await combinedTempFileStreamWriter.WriteLineAsync(line);
                    }
                }

                combinedTempFileStream.Seek(0, SeekOrigin.Begin);
                destinationFileStream.Seek(0, SeekOrigin.Begin);

                await using (var destinationFileStreamWriter = new StreamWriter(destinationFileStream, leaveOpen: true))
                using (var combinedFileStreamReader = new StreamReader(combinedTempFileStream, leaveOpen: true))
                {
                    while (!combinedFileStreamReader.EndOfStream)
                    {
                        var line = await combinedFileStreamReader.ReadLineAsync();
                        await destinationFileStreamWriter.WriteLineAsync(line);
                    }
                }

                destinationFileStream.SetLength(destinationFileStream.Position);
            }
            finally
            {
                try
                {
                    File.Delete(combinedTempFilePath);
                }
                catch (Exception)
                {
                    // don't care
                }
            }
        }

        public static async Task TrimLinesFromStartOfFile(string sourceFilePath, string destinationFilePath, int numLines)
        {
            var remainderTempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                // source file, destination file, and remainder temp file will be locked for the duration
                // of this code block (i.e. the try block)

                await using var sourceFileStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                await using var destinationFileStream = new FileStream(destinationFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await using var remainderTempFileStream = new FileStream(remainderTempFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);


                using (var sourceFileStreamReader = new StreamReader(sourceFileStream, leaveOpen: true))
                await using (var destinationFileStreamWriter = new StreamWriter(destinationFileStream, leaveOpen: true))
                await using (var remainderTempFileStreamWriter = new StreamWriter(remainderTempFileStream, leaveOpen: true))
                {
                    for (var lineNum = 0; lineNum < numLines && !sourceFileStreamReader.EndOfStream; ++lineNum)
                    {
                        var line = await sourceFileStreamReader.ReadLineAsync();
                        await destinationFileStreamWriter.WriteLineAsync(line);
                    }

                    while (!sourceFileStreamReader.EndOfStream)
                    {
                        var line = await sourceFileStreamReader.ReadLineAsync();
                        await remainderTempFileStreamWriter.WriteLineAsync(line);
                    }
                }

                sourceFileStream.Seek(0, SeekOrigin.Begin);
                remainderTempFileStream.Seek(0, SeekOrigin.Begin);

                await using (var sourceFileStreamWriter = new StreamWriter(sourceFileStream, leaveOpen: true))
                using (var remainderTempFileStreamReader = new StreamReader(remainderTempFileStream, leaveOpen: true))
                {
                    while (!remainderTempFileStreamReader.EndOfStream)
                    {
                        var line = await remainderTempFileStreamReader.ReadLineAsync();
                        await sourceFileStreamWriter.WriteLineAsync(line);
                    }
                }

                sourceFileStream.SetLength(sourceFileStream.Position);
            }
            finally
            {
                try
                {
                    File.Delete(remainderTempFilePath);
                }
                catch (Exception)
                {
                    // don't care
                }
            }
        }

        public static void DeleteFileIfEmpty(string path)
        {
            try
            {
                var fileInfo = new FileInfo(path);
                if (fileInfo.Exists && fileInfo.Length == 0)
                {
                    File.Delete(path);
                }
            }
            catch (FileNotFoundException)
            {
                // STUPENDOUS
            }
        }
    }
}