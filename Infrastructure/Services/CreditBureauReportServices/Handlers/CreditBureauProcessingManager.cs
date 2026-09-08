using Infrastructure.Services.Notifications;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.CreditBureauReportServices.Handlers;

/// <summary>
/// Менеджер обработки CI-запросов
/// Координирует работу всех обработчиков
/// </summary>
public class CreditBureauProcessingManager
{
    private readonly IEnumerable<ICiHandler> _handlers;
    private readonly ILogger<CreditBureauProcessingManager> _logger;
    private readonly ITelegramNotificationService _telegramNotificationService;

    public CreditBureauProcessingManager(
        IEnumerable<ICiHandler> handlers,
        ILogger<CreditBureauProcessingManager> logger,
        ITelegramNotificationService telegramNotificationService)
    {
        _handlers = handlers.OrderBy(h => h.CiCode).ToList();
        _logger = logger;
        _telegramNotificationService = telegramNotificationService;
    }

    /// <summary>
    /// Запуск обработки всех CI-запросов.
    /// <paramref name="runHandler"/> позволяет вызывающему коду обернуть handler
    /// (например, period-locks для CI-015/016/018). Возврат null = handler пропущен.
    /// </summary>
    public async Task ProcessAllAsync(
        Func<ICiHandler, CancellationToken, Task<CiProcessingResult?>> runHandler,
        CancellationToken cancellationToken = default)
    {
        var processingStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("CreditBureauReportProcessing started. Handlers count={HandlersCount}", _handlers.Count());

        var results = new List<(int CiCode, CiProcessingResult Result)>();

        foreach (var handler in _handlers)
        {
            try
            {
                _logger.LogInformation("Starting CI-{CiCode} handler", handler.CiCode);
                var result = await runHandler(handler, cancellationToken);
                if (result is null)
                {
                    _logger.LogInformation("CI-{CiCode} handler skipped", handler.CiCode);
                    continue;
                }

                results.Add((handler.CiCode, result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CI-{CiCode}. Error={Error}", handler.CiCode, ex.Message);
                await _telegramNotificationService.NotifyErrorAsync(
                    $"CreditBureauProcessingManager CI-{handler.CiCode:D3}",
                    $"Message: {ex.Message}\nStackTrace: {ex.StackTrace}",
                    cancellationToken);
            }
        }

        processingStopwatch.Stop();

        var totalProcessed = results.Sum(r => r.Result.Processed);
        var totalSuccess = results.Sum(r => r.Result.Success);
        var totalError = results.Sum(r => r.Result.Error);

        _logger.LogInformation(
            "CreditBureauReportProcessing completed in {ElapsedSeconds}s. Total: Processed={Processed}, Success={Success}, Error={Error}",
            processingStopwatch.Elapsed.TotalSeconds,
            totalProcessed,
            totalSuccess,
            totalError);

        foreach (var (ciCode, result) in results)
        {
            _logger.LogInformation(
                "CI-{CiCode:D3}: Processed={Processed}, Success={Success}, Error={Error}",
                ciCode, result.Processed, result.Success, result.Error);
        }
    }
}
