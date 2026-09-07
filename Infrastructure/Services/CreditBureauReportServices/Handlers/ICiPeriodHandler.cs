namespace Infrastructure.Services.CreditBureauReportServices.Handlers;

/// <summary>
/// CI handler that can re-send queue items for an explicit date period (CI-015/016/018).
/// </summary>
public interface ICiPeriodHandler : ICiHandler
{
    Task<CiProcessingResult> SendByPeriodAsync(
        DateTime startDate,
        DateTime endDate,
        int? loanKey,
        CancellationToken cancellationToken);
}
