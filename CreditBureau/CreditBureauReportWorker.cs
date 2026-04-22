using Domain.Common.Settings;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.CreditBureauReportServices;
using Infrastructure.Services.Notifications;

namespace CreditBureau;

public class CreditBureauReportWorker(
    WorkerSettings workerSettings,
    LogWriter logWriter,
    ICreditBureauReportService creditBureauReportService,
    ITelegramNotificationService telegramNotificationService,
    ILogger<CreditBureauReportWorker> logger) : BackgroundService
{
    private readonly WorkerSettings _workerSettings = workerSettings;
    private readonly LogWriter _logWriter = logWriter;
    private readonly ICreditBureauReportService _creditBureauReportService = creditBureauReportService;
    private readonly ITelegramNotificationService _telegramNotificationService = telegramNotificationService;
    private readonly ILogger<CreditBureauReportWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "CreditBureauReportWorker started. DelayMilliseconds={DelayMilliseconds}",
            _workerSettings.DelayMilliseconds);
        _logWriter.Log("WorkerServerState.txt", "Start successful!");

        var iteration = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            iteration++;
            try
            {
                _logger.LogInformation("CreditBureauReportWorker iteration {Iteration} started.", iteration);
                var startedAt = DateTime.UtcNow;
                await _creditBureauReportService.CreditBureauReportProcessing(stoppingToken);
                var elapsedMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;
                _logger.LogInformation(
                    "CreditBureauReportWorker iteration {Iteration} finished. ElapsedMs={ElapsedMs}",
                    iteration,
                    elapsedMs);
                await Task.Delay(_workerSettings.DelayMilliseconds, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "CreditBureauReportWorker cancellation requested on iteration {Iteration}.",
                    iteration);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreditBureauReportWorker iteration {Iteration} failed.", iteration);
                _logWriter.Log("WorkerServerState.txt", "Catch AsokiProcessing message error: " + ex.Message + "\n" + ex.StackTrace);
                await _telegramNotificationService.NotifyErrorAsync(
                    "CreditBureauReportWorker",
                    $"Iteration: {iteration}\nMessage: {ex.Message}\nStackTrace: {ex.StackTrace}",
                    stoppingToken);
                await Task.Delay(_workerSettings.DelayMilliseconds, stoppingToken);
            }
        }

        _logger.LogInformation("CreditBureauReportWorker stopped.");
        _logWriter.Log("WorkerServerState.txt", "Stop successful!");
    }
}
