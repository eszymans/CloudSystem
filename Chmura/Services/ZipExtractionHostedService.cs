using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Chmura.Services
{
    public class ZipExtractionHostedService : BackgroundService
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ILoggingService _loggingService;

        public ZipExtractionHostedService(IBackgroundTaskQueue taskQueue, ILoggingService loggingService)
        {
            _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var workItem = await _taskQueue.DequeueAsync(stoppingToken).ConfigureAwait(false);
                    try
                    {
                        await workItem(stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { /* anulowano */ }
                    catch (Exception ex)
                    {
                        await _loggingService.LogErrorAsync("SYSTEM", "ZIP_BG", $"B³¹d podczas wykonywania zadania rozpakowania: {ex.Message}");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await _loggingService.LogErrorAsync("SYSTEM", "ZIP_BG", $"B³¹d kolejki zadañ: {ex.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
                }
            }
        }
    }
}