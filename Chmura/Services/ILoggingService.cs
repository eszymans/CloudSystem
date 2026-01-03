using System;
using System.IO;
using System.Threading.Tasks;

namespace Chmura.Services
{
    public interface ILoggingService
    {
        Task LogLoginAsync(string username);
        Task LogLogoutAsync(string username);
        Task LogFileUploadAsync(string username, string fileName, long fileSizeBytes);
        Task LogFileDeleteAsync(string username, string fileName);
        Task LogFileDownloadAsync(string username, string fileName);
        Task LogErrorAsync(string username, string operation, string errorMessage);
    }

    public class LoggingService : ILoggingService
    {
        private readonly string _logsDirectory;

        public LoggingService(IWebHostEnvironment env)
        {
            _logsDirectory = Path.Combine(env.ContentRootPath, "Logs");
            if (!Directory.Exists(_logsDirectory))
                Directory.CreateDirectory(_logsDirectory);
        }

        public async Task LogLoginAsync(string username)
        {
            await LogAsync("logins.log", username, "LOGIN", "Zalogowany");
        }

        public async Task LogLogoutAsync(string username)
        {
            await LogAsync("logins.log", username, "LOGOUT", "Wylogowany");
        }

        public async Task LogFileUploadAsync(string username, string fileName, long fileSizeBytes)
        {
            var sizeMB = (fileSizeBytes / 1024.0 / 1024.0).ToString("F2");
            await LogAsync("file_operations.log", username, "UPLOAD", $"{fileName} ({sizeMB} MB)");
        }

        public async Task LogFileDeleteAsync(string username, string fileName)
        {
            await LogAsync("file_operations.log", username, "DELETE", fileName);
        }

        public async Task LogFileDownloadAsync(string username, string fileName)
        {
            await LogAsync("file_operations.log", username, "DOWNLOAD", fileName);
        }

        public async Task LogErrorAsync(string username, string operation, string errorMessage)
        {
            await LogAsync("errors.log", username, operation, errorMessage);
        }

        private async Task LogAsync(string logFileName, string username, string operation, string details)
        {
            try
            {
                var logFilePath = Path.Combine(_logsDirectory, logFileName);
                var nowUtc = DateTime.UtcNow;
                var franceCentralZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
                var localTime = TimeZoneInfo.ConvertTime(nowUtc, franceCentralZone);
                var timestamp = localTime.ToString("yyyy-MM-dd HH:mm:ss");
                
                var logEntry = $"{timestamp} | {username} | {operation} | {details}{Environment.NewLine}";

                await File.AppendAllTextAsync(logFilePath, logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas logowania: {ex.Message}");
            }
        }
    }
}