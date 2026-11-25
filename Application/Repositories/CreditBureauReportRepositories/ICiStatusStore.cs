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

    /// <summary>
    /// Resolves <c>Loan.[key]</c> (Katm_Methods_Request.loanKey) from <c>Loan_History_KB.[key]</c>
    /// via <c>Loan_History_KB.App</c> → <c>Loan.App</c>.
    /// </summary>
    Task<int?> GetLoanKeyByHistoryKbAsync(int keyLoanHistoryKb, CancellationToken cancellationToken);

    Task<(string? App, string? CustomerId)> GetLoanAppAndCustomerIdAsync(
        string loanKey,
        CancellationToken cancellationToken);
}
