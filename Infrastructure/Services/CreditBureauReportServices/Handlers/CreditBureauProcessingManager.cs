using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.CreditBureauReportServices.Handlers;

/// <summary>
/// Менеджер обработки CI-запросов
/// Координирует выполнение всех обработчиков
/// </summary>
public class CreditBureauProcessingManager
{
    private readonly IEnumerable<ICiHandler> _handlers;
    private readonly ILogger<CreditBureauProcessingManager> _logger;

    public CreditBureauProcessingManager(
        IEnumerable<ICiHandler> handlers,
        ILogger<CreditBureauProcessingManager> logger)
    {
        _handlers = handlers.OrderBy(h => h.CiCode).ToList();
        _logger = logger;
    }

    /// <summary>
    /// Запуск обработки всех CI-запросов
    /// </summary>
    public async Task ProcessAllAsync(CancellationToken cancellationToken = default)
    {
        var processingStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("CreditBureauReportProcessing started. Handlers count={HandlersCount}", _handlers.Count());

        var results = new List<(int CiCode, CiProcessingResult Result)>();

        foreach (var handler in _handlers)
        {
            try
            {
                _logger.LogInformation("Starting CI-{CiCode} handler", handler.CiCode);
                var result = await handler.ProcessAsync(cancellationToken);
                results.Add((handler.CiCode, result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CI-{CiCode}. Error={Error}", handler.CiCode, ex.Message);
            }
        }

        processingStopwatch.Stop();

        // Итоговая статистика
        var totalProcessed = results.Sum(r => r.Result.Processed);
        var totalSuccess = results.Sum(r => r.Result.Success);
        var totalError = results.Sum(r => r.Result.Error);

        _logger.LogInformation(
            "CreditBureauReportProcessing completed in {ElapsedSeconds}s. Total: Processed={Processed}, Success={Success}, Error={Error}",
            processingStopwatch.Elapsed.TotalSeconds,
            totalProcessed,
            totalSuccess,
            totalError);

        // Детальная статистика по каждому CI
        foreach (var (ciCode, result) in results)
        {
            _logger.LogInformation(
                "CI-{CiCode:D3}: Processed={Processed}, Success={Success}, Error={Error}",
                ciCode, result.Processed, result.Success, result.Error);
        }
    }
}
