
using CreditBureauService.Contracts.CreditBureauApplications;

namespace Infrastructure.CreditReportsXml
{
    public interface ICreditReportXmlService
    {
        Task CreditReportXml(LoanApplication loanApplications, CancellationToken cancellationToken);
        Task CreditReportStatusXml(LoanApplication loanApplications, CancellationToken cancellationToken);
    }
}
