using Application.Repositories.CreditBureauReportRepositories;

namespace Infrastructure.Services.CreditBureauReportServices.Handlers;

/// <summary>
/// Интерфейс обработчика CI-запросов
/// </summary>
public interface ICiHandler
{
    /// <summary>
    /// Код CI (например, 1 для CI-001)
    /// </summary>
    int CiCode { get; }

    /// <summary>
    /// Обработка очереди запросов
    /// </summary>
    Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Результат обработки CI-запросов
/// </summary>
public class CiProcessingResult
{
    public int Processed { get; set; }
    public int Success { get; set; }
    public int Error { get; set; }
    public int Pending { get; set; }
}
