namespace Application.Repositories.CreditBureauReportRepositories;

/// <summary>
/// CI-017 attempt counter, request-history status, and request log persistence.
/// </summary>
public interface ICi017StateStore
{
    Task<Ci017State> GetCi017StateAsync(int loanKey, string? requestSubtype, CancellationToken cancellationToken);

    Task UpdateRequestHistoryStatusAsync(int loanKey, string status, CancellationToken cancellationToken, string? requestSubtype = null, string? comment = null);

    Task IncrementCi017AttemptAsync(int loanKey, string? requestSubtype, string? currentStatus, CancellationToken cancellationToken);

    Task InsertCi017RequestLogAsync(
        int loanKey,
        string? claimId,
        string requestType,
        int attemptNumber,
        string? requestBody,
        string? responseBody,
        DateTime dateRequest,
        DateTime? dateResponse,
        CancellationToken cancellationToken);
}
