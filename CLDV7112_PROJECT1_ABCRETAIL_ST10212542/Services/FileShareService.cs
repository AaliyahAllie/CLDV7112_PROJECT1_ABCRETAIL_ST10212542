using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Services
{
    public class FileShareService
    {
        private readonly ShareClient _shareClient;
        private readonly ShareDirectoryClient _directoryClient;
        private readonly ShareFileClient _fileClient;
        private const string ShareName = "logs-share";
        private const string LogFileName = "abc-retail-logs.txt";

        public FileShareService(string connectionString)
        {
            _shareClient = new ShareClient(connectionString, ShareName);
            _shareClient.CreateIfNotExists();

            _directoryClient = _shareClient.GetRootDirectoryClient();
            _fileClient = _directoryClient.GetFileClient(LogFileName);

            if (!_fileClient.Exists())
            {
                // Create an empty file. Initialize with 0 size
                _fileClient.Create(0);
                AppendLogLineInternal("INFO", "Log file initialized in Azure File Share.");
            }
        }

        public void AppendLog(string level, string message)
        {
            try
            {
                AppendLogLineInternal(level, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write to File Share: {ex.Message}");
            }
        }

        public async Task AppendLogAsync(string level, string message)
        {
            try
            {
                await Task.Run(() => AppendLogLineInternal(level, message));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write to File Share: {ex.Message}");
            }
        }

        private void AppendLogLineInternal(string level, string message)
        {
            string logLine = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} [{level}] {message}\n";
            string existingContent = "";

            if (_fileClient.Exists())
            {
                var properties = _fileClient.GetProperties();
                if (properties.Value.ContentLength > 0)
                {
                    var download = _fileClient.Download();
                    using (var reader = new StreamReader(download.Value.Content, Encoding.UTF8))
                    {
                        existingContent = reader.ReadToEnd();
                    }
                }
            }

            string newContent = existingContent + logLine;
            byte[] bytes = Encoding.UTF8.GetBytes(newContent);

            // Recreate the file with the new exact content length
            _fileClient.Create(bytes.Length);

            // Upload the entire new file content
            using (var stream = new MemoryStream(bytes))
            {
                _fileClient.UploadRange(new HttpRange(0, bytes.Length), stream);
            }
        }

        public async Task<List<LogEntry>> ReadLogsAsync()
        {
            var logs = new List<LogEntry>();
            if (!await _fileClient.ExistsAsync())
            {
                return logs;
            }

            var properties = await _fileClient.GetPropertiesAsync();
            long contentLength = properties.Value.ContentLength;
            if (contentLength == 0)
            {
                return logs;
            }

            var download = await _fileClient.DownloadAsync();
            using (var reader = new StreamReader(download.Value.Content, Encoding.UTF8))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    try
                    {
                        var parts = line.Split(new[] { ' ' }, 3);
                        if (parts.Length >= 3)
                        {
                            var timeStr = parts[0] + " " + parts[1];
                            var timestamp = DateTime.Parse(timeStr);
                            var levelWithBrackets = parts[2].Split(new[] { ']' }, 2);
                            var level = levelWithBrackets[0].Replace("[", "");
                            var msg = levelWithBrackets[1].Trim();

                            logs.Add(new LogEntry
                            {
                                Timestamp = timestamp,
                                Level = level,
                                Message = msg
                            });
                        }
                        else
                        {
                            logs.Add(new LogEntry
                            {
                                Timestamp = DateTime.UtcNow,
                                Level = "INFO",
                                Message = line
                            });
                        }
                    }
                    catch
                    {
                        logs.Add(new LogEntry
                        {
                            Timestamp = DateTime.UtcNow,
                            Level = "INFO",
                            Message = line
                        });
                    }
                }
            }

            logs.Reverse(); // Display latest logs first
            return logs;
        }

        public async Task ClearLogsAsync()
        {
            if (await _fileClient.ExistsAsync())
            {
                await _fileClient.DeleteAsync();
                await _fileClient.CreateAsync(0);
                await AppendLogAsync("INFO", "Log file cleared and reinitialized.");
            }
        }
    }
}
