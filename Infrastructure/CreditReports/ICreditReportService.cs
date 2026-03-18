
using CreditBureau.Contracts.AsokiLoanApplications;

namespace Infrastructure.CreditReports
{
    public interface ICreditReportService
    {
        Task CreditReport(LoanApplication loanApplications, CancellationToken cancellationToken);
        Task CreditReportStatus(LoanApplication loanApplications, CancellationToken cancellationToken);
    }
}
