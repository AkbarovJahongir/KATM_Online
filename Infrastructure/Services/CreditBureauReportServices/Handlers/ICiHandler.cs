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
    public List<CiProcessingDetail> Details { get; set; } = new();

    public void AddDetail(int loanKey, bool isSuccess, string message, string? rawResponse = null)
    {
        Details.Add(new CiProcessingDetail
        {
            LoanKey = loanKey,
            IsSuccess = isSuccess,
            Message = message,
            RawResponse = rawResponse
        });
    }
}

public class CiProcessingDetail
{
    public int LoanKey { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RawResponse { get; set; }
}
