using Infrastructure.Services.CreditBureauReportServices.Handlers;
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
    /// </summary>
    public async Task CreditBureauReportProcessing(CancellationToken cancellationToken)
    {
        await _processingManager.ProcessAllAsync(cancellationToken);
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
    /// </summary>
    /// <param name="startDate">Дата начала периода</param>
    /// <param name="endDate">Дата окончания периода</param>
    /// <param name="loanKey">Фильтр по LoanKey (необязательно)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task<CiProcessingResult> SendCi015ByPeriodAsync(DateTime startDate, DateTime endDate, int? loanKey, CancellationToken cancellationToken = default)
    {
        var handler = _handlers.OfType<Ci015RepaymentRequestHandler>().FirstOrDefault();
        if (handler is null)
        {
            _logger.LogWarning("CI-015 handler not found");
            return new CiProcessingResult();
        }

        _logger.LogInformation("Sending CI-015 for period {StartDate} - {EndDate}, LoanKey: {LoanKey}", startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"), loanKey);
        return await handler.SendByPeriodAsync(startDate, endDate, loanKey, cancellationToken);
    }

    /// <summary>
    /// Отправка отчета CI-016 (Сведения о платежных документах) за указанный период
    /// </summary>
    /// <param name="startDate">Дата начала периода</param>
    /// <param name="endDate">Дата окончания периода</param>
    /// <param name="loanKey">Фильтр по LoanKey (необязательно)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task<CiProcessingResult> SendCi016ByPeriodAsync(DateTime startDate, DateTime endDate, int? loanKey, CancellationToken cancellationToken = default)
    {
        var handler = _handlers.OfType<Ci016BankDetailRequestHandler>().FirstOrDefault();
        if (handler is null)
        {
            _logger.LogWarning("CI-016 handler not found");
            return new CiProcessingResult();
        }

        _logger.LogInformation("Sending CI-016 for period {StartDate} - {EndDate}, LoanKey: {LoanKey}", startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"), loanKey);
        return await handler.SendByPeriodAsync(startDate, endDate, loanKey, cancellationToken);
    }

    /// <summary>
    /// Отправка отчета CI-018 (Сведения о статусе счетов) за указанный период
    /// </summary>
    /// <param name="startDate">Дата начала периода</param>
    /// <param name="endDate">Дата окончания периода</param>
    /// <param name="loanKey">Фильтр по LoanKey (необязательно)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task<CiProcessingResult> SendCi018ByPeriodAsync(DateTime startDate, DateTime endDate, int? loanKey, CancellationToken cancellationToken = default)
    {
        var handler = _handlers.OfType<Ci018AccountStatusRequestHandler>().FirstOrDefault();
        if (handler is null)
        {
            _logger.LogWarning("CI-018 handler not found");
            return new CiProcessingResult();
        }

        _logger.LogInformation("Sending CI-018 for period {StartDate} - {EndDate}, LoanKey: {LoanKey}", startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"), loanKey);
        return await handler.SendByPeriodAsync(startDate, endDate, loanKey, cancellationToken);
    }
}
