using Domain.Common.Settings;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.CreditBureauServices;
using Infrastructure.Services.Notifications;

namespace CreditBureauService
{
    public class CreditBureauWorker(WorkerSettings workerSettings, LogWriter logWriter, ICreditBureauService creditBureauService, ITelegramNotificationService telegramNotificationService) : BackgroundService
    {
        private readonly WorkerSettings _workerSettings = workerSettings;
        private readonly LogWriter _logWriter = logWriter;
        private readonly ICreditBureauService _creditBureauService = creditBureauService;
        private readonly ITelegramNotificationService _telegramNotificationService = telegramNotificationService;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logWriter.Log("WorkerServerState.txt", "Start successful!");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _creditBureauService.CreditBureauProcessing(stoppingToken);
                    await Task.Delay(_workerSettings.DelayMilliseconds, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logWriter.Log("WorkerServerState.txt", "Catch CreditBureauProcessing message error: " + ex.Message + "\n" + ex.StackTrace);
                    await _telegramNotificationService.NotifyErrorAsync(
                        "CreditBureauWorker",
                        $"Message: {ex.Message}\nStackTrace: {ex.StackTrace}",
                        stoppingToken);
                    await Task.Delay(_workerSettings.DelayMilliseconds, stoppingToken);
                }
            }
            _logWriter.Log("WorkerServerState.txt", "Stop successful!");
        }
    }
}