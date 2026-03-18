
using CreditBureau.Contracts.AsokiLoanApplications;

namespace Infrastructure.CreditReportsXml
{
    public interface ICreditReportXmlService
    {
        Task CreditReportXml(LoanApplication loanApplications, CancellationToken cancellationToken);
        Task CreditReportStatusXml(LoanApplication loanApplications, CancellationToken cancellationToken);
    }
}
