using Domain.Common.Settings;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.CreditBureauXmlServices;
using Infrastructure.Services.Notifications;

namespace CreditBureauService
{
    public class CreditBureauWorkerXml(WorkerSettings workerSettings, LogWriter logWriter, ICreditBureauXmlService creditBureauXmlService, ITelegramNotificationService telegramNotificationService) : BackgroundService
    {
        private readonly WorkerSettings _workerSettings = workerSettings;
        private readonly LogWriter _logWriter = logWriter;
        private readonly ICreditBureauXmlService _creditBureauXmlService = creditBureauXmlService;
        private readonly ITelegramNotificationService _telegramNotificationService = telegramNotificationService;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logWriter.Log("WorkerServerStateXml.txt", "Start successful!");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _creditBureauXmlService.CreditBureauProcessingXml(stoppingToken);
                    await Task.Delay(_workerSettings.DelayMilliseconds, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logWriter.Log("WorkerServerStateXml.txt", "Catch CreditBureauProcessingXml message error: " + ex.Message + "\n" + ex.StackTrace);
                    await _telegramNotificationService.NotifyErrorAsync(
                        "CreditBureauWorkerXml",
                        $"Message: {ex.Message}\nStackTrace: {ex.StackTrace}",
                        stoppingToken);
                    await Task.Delay(_workerSettings.DelayMilliseconds, stoppingToken);
                }
            }
            _logWriter.Log("WorkerServerStateXml.txt", "Stop successful!");
        }
    }
}