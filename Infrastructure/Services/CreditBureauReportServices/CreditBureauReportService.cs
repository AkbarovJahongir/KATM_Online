using Infrastructure.Services.CreditBureauReportServices.Handlers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.CreditBureauReportServices;

/// <summary>
/// Рефакторенный сервис обработки кредитных бюро отчетов
/// Использует паттерн Chain of Responsibility с отдельными обработчиками для каждого CI-кода
/// </summary>
public class CreditBureauReportService : ICreditBureauReportService
{
    private readonly CreditBureauProcessingManager _processingManager;
    private readonly IEnumerable<ICiHandler> _handlers;
    private readonly ILogger<CreditBureauReportService> _logger;
    private readonly SemaphoreSlim _processingLock = new SemaphoreSlim(1, 1);

    private readonly SemaphoreSlim _ci015Lock = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim _ci016Lock = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim _ci018Lock = new SemaphoreSlim(1, 1);

    public CreditBureauReportService(
        CreditBureauProcessingManager processingManager,
        IEnumerable<ICiHandler> handlers,
        ILogger<CreditBureauReportService> logger)
    {
        _processingManager = processingManager;
        _handlers = handlers;
        _logger = logger;
    }

    /// <summary>
    /// Обработка всех CI-запросов
    /// Предотвращает параллельные итерации, которые могут привести к deadlock-у
    /// </summary>
    public async Task CreditBureauReportProcessing(CancellationToken cancellationToken)
    {
        if (!await _processingLock.WaitAsync(0, cancellationToken))
        {
            _logger.LogInformation("CreditBureauReportProcessing is already running, skipping iteration");
            return;
        }

        try
        {
            await ProcessWithRetryAsync(cancellationToken);
        }
        finally
        {
            _processingLock.Release();
        }
    }

    private async Task ProcessWithRetryAsync(CancellationToken cancellationToken, int attemptNumber = 1, int maxAttempts = 3)
    {
        try
        {
            await _processingManager.ProcessAllAsync(cancellationToken);
        }
        catch (SqlException ex) when (ex.Number == 1205 && attemptNumber < maxAttempts)
        {
            var delayMs = (int)Math.Pow(2, attemptNumber) * 1000;
            _logger.LogWarning(ex, "Deadlock detected in CreditBureauReportProcessing (attempt {AttemptNumber}/{MaxAttempts}), retrying after {DelayMs}ms",
                attemptNumber, maxAttempts, delayMs);

            await Task.Delay(delayMs, cancellationToken);
            await ProcessWithRetryAsync(cancellationToken, attemptNumber + 1, maxAttempts);
        }
    }

    /// <summary>
    /// Обработка конкретного CI-запроса
    /// </summary>
    /// <param name="ciCode">Код CI (например, 1 для CI-001)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат обработки</returns>
    public async Task<CiProcessingResult> ProcessCiCodeAsync(int ciCode, CancellationToken cancellationToken = default)
    {
        var handler = _handlers.FirstOrDefault(h => h.CiCode == ciCode);

        if (handler is null)
        {
            _logger.LogWarning("Handler for CI-{CiCode} not found", ciCode);
            return new CiProcessingResult();
        }

        _logger.LogInformation("Processing CI-{CiCode} individually", ciCode);
        return await handler.ProcessAsync(cancellationToken);
    }

    /// <summary>
    /// Отправка отчета CI-015 (Сведения об остатках на счетах) за указанный период
    /// Синхронизирована чтобы избежать deadlock-а с worker-ом
    /// </summary>
    /// <param name="startDate">Дата начала периода</param>
    /// <param name="endDate">Дата окончания периода</param>
    /// <param name="loanKey">Фильтр по LoanKey (необязательно)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task<CiProcessingResult> SendCi015ByPeriodAsync(DateTime startDate, DateTime endDate, int? loanKey, CancellationToken cancellationToken = default)
    {
        return await SendPeriodAsync(15, _ci015Lock, startDate, endDate, loanKey, cancellationToken);
    }

    /// <summary>
    /// Отправка отчета CI-016 (Сведения о платежных документах) за указанный период
    /// Синхронизирована чтобы избежать deadlock-а с worker-ом
    /// </summary>
    public async Task<CiProcessingResult> SendCi016ByPeriodAsync(DateTime startDate, DateTime endDate, int? loanKey, CancellationToken cancellationToken = default)
    {
        return await SendPeriodAsync(16, _ci016Lock, startDate, endDate, loanKey, cancellationToken);
    }

    /// <summary>
    /// Отправка отчета CI-018 (Сведения о статусе счетов) за указанный период
    /// Синхронизирована чтобы избежать deadlock-а с worker-ом
    /// </summary>
    public async Task<CiProcessingResult> SendCi018ByPeriodAsync(DateTime startDate, DateTime endDate, int? loanKey, CancellationToken cancellationToken = default)
    {
        return await SendPeriodAsync(18, _ci018Lock, startDate, endDate, loanKey, cancellationToken);
    }

    private async Task<CiProcessingResult> SendPeriodAsync(
        int ciCode,
        SemaphoreSlim gate,
        DateTime startDate,
        DateTime endDate,
        int? loanKey,
        CancellationToken cancellationToken)
    {
        var handler = _handlers.OfType<ICiPeriodHandler>().FirstOrDefault(h => h.CiCode == ciCode);
        if (handler is null)
        {
            _logger.LogWarning("CI-{CiCode} period handler not found", ciCode);
            return new CiProcessingResult();
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation(
                "Sending CI-{CiCode} for period {StartDate} - {EndDate}, LoanKey: {LoanKey}",
                ciCode, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"), loanKey);
            return await handler.SendByPeriodAsync(startDate, endDate, loanKey, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }
}
