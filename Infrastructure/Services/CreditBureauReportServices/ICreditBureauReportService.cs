using Infrastructure.Services.CreditBureauReportServices.Handlers;

namespace Infrastructure.Services.CreditBureauReportServices;

public interface ICreditBureauReportService
{
    Task CreditBureauReportProcessing() => CreditBureauReportProcessing(CancellationToken.None);
    Task CreditBureauReportProcessing(CancellationToken cancellationToken);
    Task<CiProcessingResult> ProcessCiCodeAsync(int ciCode, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отправка отчета CI-015 (Сведения об остатках на счетах) за указанный период
    /// </summary>
    /// <param name="startDate">Дата начала периода</param>
    /// <param name="endDate">Дата окончания периода</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task<CiProcessingResult> SendCi015ByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отправка отчета CI-016 (Сведения о платежных документах) за указанный период
    /// </summary>
    /// <param name="startDate">Дата начала периода</param>
    /// <param name="endDate">Дата окончания периода</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task<CiProcessingResult> SendCi016ByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отправка отчета CI-018 (Сведения о статусе счетов) за указанный период
    /// </summary>
    /// <param name="startDate">Дата начала периода</param>
    /// <param name="endDate">Дата окончания периода</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task<CiProcessingResult> SendCi018ByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
