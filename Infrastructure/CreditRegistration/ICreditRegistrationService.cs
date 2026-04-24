using CreditBureauService.Contracts.CreditBureauApplications;

namespace Infrastructure.CreditRegistration
{
    public interface ICreditRegistrationService
    {
        Task SenderClaimsAsync(LoanApplication loanApplications, CancellationToken cancellationToken);
        Task SenderClaimsXmlAsync(LoanApplication loanApplications, CancellationToken cancellationToken);
    }
}
