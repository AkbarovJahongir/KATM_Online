using CreditBureauService.Contracts.CreditBureauApplications;

namespace Application.Repositories.CreditBureauRepositories
{
    public interface ICreditBureauRepository
    {
        Task<List<LoanApplication>> GetLoanApplications(CancellationToken cancellationToken);
        Task<List<LoanApplication>> GetLoanApplicationsXml(CancellationToken cancellationToken);

        Task UpdateRequestHistoryXmlStatusAsync(string keyCreditBureauKb, string status, CancellationToken cancellationToken);

        Task UpdateRequestHistoryStatusAsync(string keyCreditBureauKb, string status, CancellationToken cancellationToken);
    }
}