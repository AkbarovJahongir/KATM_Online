namespace Application.Repositories.CreditBureauReportRepositories;

/// <summary>
/// CI status upserts and prerequisite / loan lookup helpers.
/// </summary>
public interface ICiStatusStore
{
    Task UpsertCiStatusAsync(
        int loanKey,
        int ciCode,
        byte ciStatus,
        string? message,
        string? token,
        CancellationToken cancellationToken);

    Task<byte?> GetCreditBureau001StatusAsync(int loanKey, CancellationToken cancellationToken);

    Task<byte?> GetCreditBureau002StatusAsync(int loanKey, CancellationToken cancellationToken);

    Task<(string? App, string? CustomerId)> GetLoanAppAndCustomerIdAsync(
        string loanKey,
        CancellationToken cancellationToken);
}
